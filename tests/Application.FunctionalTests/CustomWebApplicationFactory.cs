using System.Data.Common;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Infrastructure.Data;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AskAMuslimBackend.Application.FunctionalTests;

using static Testing;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;
    private readonly string _connectionString;

    public CustomWebApplicationFactory(DbConnection connection, string connectionString)
    {
        _connection = connection;
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseEnvironment("Testing")
            .UseSetting("ConnectionStrings:AskAMuslimBackendDb", _connectionString);

        builder.ConfigureTestServices(services =>
        {
            services
                .RemoveAll<IUser>()
                .AddTransient(provider =>
                {
                    var mock = new Mock<IUser>();
                    mock.SetupGet(x => x.Roles).Returns(GetRoles());
                    mock.SetupGet(x => x.Id).Returns(GetUserId());
                    return mock.Object;
                });
            // Never send real emails, push notifications or Hangfire jobs from tests; record them instead.
            services
                .RemoveAll<IEmailService>()
                .AddScoped(_ => EmailService.Object);
            services
                .RemoveAll<IFirebaseMessagingService>()
                .AddScoped(_ => FirebaseMessaging.Object);
            services
                .RemoveAll<IBackgroundJobClient>()
                .AddSingleton(_ => BackgroundJobClient.Object);
            services
                .RemoveAll<DbContextOptions<ApplicationDbContext>>()
                .AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                    options.UseNpgsql(_connection);
                });
        });
    }
}
