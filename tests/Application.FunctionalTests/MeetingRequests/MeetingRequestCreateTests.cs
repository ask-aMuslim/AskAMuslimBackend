using AskAMuslimBackend.Application.Common.Exceptions;
using AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.FunctionalTests.MeetingRequests;

using static Testing;

public class MeetingRequestCreateTests : BaseTestFixture
{
    private static CreateMeetingRequestCommand RequestFor(DateTime scheduledAt) => new()
    {
        Topic = MeetingInquiryTopic.Fiqh,
        Message = "A private question",
        Languages = new List<Language> { Language.English },
        ScheduledAt = scheduledAt
    };

    [SetUp]
    public async Task SignIn() => await RunAsUserAsync("owner@local", "Testing1234!", UserRole.Student);

    [Test]
    public async Task Create_WithEnoughNotice_Succeeds()
    {
        var result = await SendAsync(RequestFor(DateTime.UtcNow.AddHours(25)));

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
    }

    [Test]
    public async Task Create_ExactlyAtTheNoticeLimit_Succeeds_EvenWithASlightlySlowClient()
    {
        // A client that checked the limit a few seconds ago must not be refused.
        var result = await SendAsync(RequestFor(DateTime.UtcNow.AddHours(24).AddSeconds(-30)));

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
    }

    [Test]
    public async Task Create_WithTooLittleNotice_IsRefused_WithAClearMessage()
    {
        var exception = await Should.ThrowAsync<ValidationException>(() =>
            SendAsync(RequestFor(DateTime.UtcNow.AddHours(23))));

        exception.Errors[nameof(CreateMeetingRequestCommand.ScheduledAt)]
            .ShouldContain("Scheduled time must be at least 24 hours from now.");
    }

    [Test]
    public async Task Create_ForALaterTimeToday_IsRefused()
    {
        // What the Android app can currently offer: a slot a few hours from now.
        await Should.ThrowAsync<ValidationException>(() => SendAsync(RequestFor(DateTime.UtcNow.AddHours(3))));
    }

    [Test]
    public async Task Create_InThePast_IsRefused()
    {
        await Should.ThrowAsync<ValidationException>(() => SendAsync(RequestFor(DateTime.UtcNow.AddHours(-1))));
    }

    [Test]
    public void Earliest_StartAllowsTheToleranceBelowTheNoticePeriod()
    {
        var now = new DateTimeOffset(2026, 10, 23, 10, 0, 0, TimeSpan.Zero);

        MeetingRequestRules.EarliestStartUtc(now).ShouldBe(new DateTime(2026, 10, 24, 9, 55, 0, DateTimeKind.Utc));
    }
}
