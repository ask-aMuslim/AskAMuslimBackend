using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Infrastructure.Services;
using FirebaseAdmin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Shouldly;

namespace AskAMuslimBackend.Application.UnitTests.Notifications;

public class FirebaseMessagingServiceTests
{
    [Test]
    public async Task SendToTokensAsync_WhenFirebaseNotInitialized_ReturnsFailedResult()
    {
        FirebaseApp.DefaultInstance.ShouldBeNull();
        var service = new FirebaseMessagingService(NullLogger<FirebaseMessagingService>.Instance);

        var result = await service.SendToTokensAsync(
            new[] { "token-a", "token-b", "token-a" },
            new PushNotificationRequest { Title = "Title", Body = "Body" });

        result.IsSuccess.ShouldBeFalse();
        result.SuccessCount.ShouldBe(0);
        result.FailureCount.ShouldBe(2);
        result.InvalidTokens.ShouldBeEmpty();
        result.Errors.ShouldBe(new[] { FirebaseMessagingService.FirebaseNotInitializedError });
    }

    [Test]
    public void Initialize_WithoutCredential_ReportsProblem()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Firebase:CredentialPath"] = "missing-firebase-key.json" })
            .Build();

        var result = FirebaseAppInitializer.Initialize(configuration, AppContext.BaseDirectory);

        result.IsInitialized.ShouldBeFalse();
        result.Problem.ShouldNotBeNull().ShouldContain("missing-firebase-key.json");
        FirebaseApp.DefaultInstance.ShouldBeNull();
    }
}
