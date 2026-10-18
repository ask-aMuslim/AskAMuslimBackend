using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Infrastructure.Data;
using AskAMuslimBackend.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace AskAMuslimBackend.Application.FunctionalTests;

[SetUpFixture]
public partial class Testing
{
    private static ITestDatabase _database = null!;
    private static CustomWebApplicationFactory _factory = null!;
    private static IServiceScopeFactory _scopeFactory = null!;
    private static string? _userId;
    private static List<string>? _roles;

    [OneTimeSetUp]
    public async Task RunBeforeAnyTests()
    {
        _database = await TestDatabaseFactory.CreateAsync();

        _factory = new CustomWebApplicationFactory(_database.GetConnection(), _database.GetConnectionString());

        _scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }

    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = _scopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await mediator.Send(request);
    }

    public static string? GetUserId()
    {
        return _userId;
    }

    public static List<string>? GetRoles()
    {
        return _roles;
    }

    public static async Task<string> RunAsDefaultUserAsync()
    {
        return await RunAsUserAsync("test@local", "Testing1234!", Array.Empty<string>());
    }

    public static async Task<string> RunAsAdministratorAsync()
    {
        return await RunAsUserAsync("administrator@local", "Administrator1234!", new[] { Roles.Administrator });
    }

    public static Task<string> RunAsUserAsync(string email, string password, string[] roles)
    {
        var role = roles.Contains(Roles.Administrator) ? UserRole.Admin : UserRole.Student;
        return RunAsUserAsync(email, password, role, roles);
    }

    // Create (or reuse) a user via UserManager and make it the current test user.
    // UserRole drives the app's permission defaults; roles only feed the mocked IUser.Roles.
    public static async Task<string> RunAsUserAsync(string email, string password, UserRole role, string[]? roles = null)
    {
        using var scope = _scopeFactory.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                FirstName = email.Split('@')[0],
                LastName = "Tester",
                Role = role,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create test user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        _userId = user.Id.ToString();
        _roles = new List<string>(roles ?? Array.Empty<string>());
        return _userId;
    }

    public static async Task ResetState()
    {
        try
        {
            await _database.ResetAsync();
        }
        catch (Exception)
        {
        }

        _userId = null;
        ResetFakes();
    }

    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.FindAsync<TEntity>(keyValues);
    }

    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Add(entity);

        await context.SaveChangesAsync();
    }

    public static async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await action(context);
    }

    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Set<TEntity>().CountAsync();
    }

    [OneTimeTearDown]
    public async Task RunAfterAnyTests()
    {
        await _database.DisposeAsync();
        await _factory.DisposeAsync();
    }

    // ---- Shared fakes for outbound side effects (emails, pushes, Hangfire jobs); reset before every test ----

    public static Mock<IEmailService> EmailService { get; } = new();
    public static Mock<IFirebaseMessagingService> FirebaseMessaging { get; } = new();
    public static Mock<IBackgroundJobClient> BackgroundJobClient { get; } = new();

    /// <summary>Jobs queued through Hangfire that have not been run yet.</summary>
    public static List<Job> EnqueuedJobs { get; } = new();

    /// <summary>Every job queued since the last reset, including ones already run.</summary>
    public static List<Job> AllEnqueuedJobs { get; } = new();

    public static void ResetFakes()
    {
        EnqueuedJobs.Clear();
        AllEnqueuedJobs.Clear();

        EmailService.Reset();

        FirebaseMessaging.Reset();
        FirebaseMessaging
            .Setup(f => f.SendToTokensAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<PushNotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string> tokens, PushNotificationRequest _, CancellationToken _) =>
                new PushNotificationResult { SuccessCount = tokens.Count() });

        BackgroundJobClient.Reset();
        BackgroundJobClient
            .Setup(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback((Job job, IState _) =>
            {
                EnqueuedJobs.Add(job);
                AllEnqueuedJobs.Add(job);
            })
            .Returns(() => Guid.NewGuid().ToString());
    }

    /// <summary>
    /// Runs queued jobs the way Hangfire would (resolving the job class from DI), including jobs they queue, until none are left.
    /// </summary>
    public static async Task RunEnqueuedJobsAsync()
    {
        while (EnqueuedJobs.Count > 0)
        {
            var job = EnqueuedJobs[0];
            EnqueuedJobs.RemoveAt(0);
            await RunJobAsync(job);
        }
    }

    public static async Task RunJobAsync(Job job)
    {
        using var scope = _scopeFactory.CreateScope();
        var instance = ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, job.Type);
        var result = job.Method.Invoke(instance, job.Args.ToArray());
        if (result is Task task)
        {
            await task;
        }
    }
}
