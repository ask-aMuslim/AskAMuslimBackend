using AskAMuslimBackend.Application.MeetingRequests.Commands.AssignMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestResponse;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestStatus;
using AskAMuslimBackend.Application.MeetingRequests.Common;
using AskAMuslimBackend.Application.Notifications.Commands.RegisterFcmToken;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.FunctionalTests.MeetingRequests;

using static Testing;

public class MeetingRequestNotificationTests : BaseTestFixture
{
    private const string Password = "Testing1234!";
    private const string RequesterEmail = "requester@local";
    private const string InboxEmail = SupportMailNotificationJobs.DefaultInboxEmail;

    private static Task<string> RunAsRequesterAsync() => RunAsUserAsync(RequesterEmail, Password, UserRole.Student);
    private static Task<string> RunAsAdminAsync() => RunAsUserAsync("admin@local", Password, UserRole.Admin);

    private static async Task<Guid> CreatePreacherAsync(string email)
    {
        var userId = Guid.Parse(await RunAsUserAsync(email, Password, UserRole.Preacher));
        await AddAsync(new Preacher { UserId = userId });
        return userId;
    }

    private static async Task<Guid> CreateRequestAsync()
    {
        var result = await SendAsync(new CreateMeetingRequestCommand
        {
            Topic = MeetingInquiryTopic.Fiqh,
            Message = "Can we talk about <b>prayer</b>?",
            Languages = new List<Language> { Language.English },
            ScheduledAt = DateTime.UtcNow.AddDays(3)
        });
        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        return result.Data;
    }

    private static Task<List<Notification>> NotificationsForAsync(Guid userId)
    {
        var list = new List<Notification>();
        return ExecuteDbContextAsync(async context =>
                list.AddRange(await context.Notifications.Where(n => n.UserId == userId).ToListAsync()))
            .ContinueWith(_ => list);
    }

    private static void ShouldHaveEmailed(string to, string subjectContains, Times? times = null) =>
        EmailService.Verify(e => e.SendEmailAsync(to, It.Is<string>(s => s.Contains(subjectContains)), It.IsAny<string>(), true), times ?? Times.Once());

    private static void ShouldHavePushedTo(IEnumerable<string> tokens, Times? times = null) =>
        FirebaseMessaging.Verify(f => f.SendToTokensAsync(
            It.Is<IEnumerable<string>>(t => t.OrderBy(x => x).SequenceEqual(tokens.OrderBy(x => x))),
            It.Is<PushNotificationRequest>(p => p.CustomData != null && p.CustomData["type"] == "meeting_request"),
            It.IsAny<CancellationToken>()), times ?? Times.Once());

    [Test]
    public async Task Create_QueuesJob_AndEmailsReceiptAndInbox_WithoutSendingInRequest()
    {
        await RunAsRequesterAsync();
        var requestId = await CreateRequestAsync();

        // Nothing is sent inside the request, only queued.
        EmailService.Invocations.ShouldBeEmpty();
        var dispatch = EnqueuedJobs.Single();
        dispatch.Method.Name.ShouldBe(nameof(MeetingRequestNotificationJobs.DispatchAsync));
        dispatch.Args[0].ShouldBe(MeetingRequestNotificationEvent.Created);
        dispatch.Args[1].ShouldBe(requestId);

        await RunEnqueuedJobsAsync();

        ShouldHaveEmailed(RequesterEmail, "We received your meeting request");
        ShouldHaveEmailed(InboxEmail, "New meeting request");
        EmailService.Verify(e => e.SendEmailAsync(InboxEmail, It.IsAny<string>(),
            It.Is<string>(body => body.Contains("&lt;b&gt;prayer&lt;/b&gt;")), true), Times.Once());
        FirebaseMessaging.Invocations.ShouldBeEmpty();
    }

    [Test]
    public async Task Assign_NotifiesModerator_InAppPushAndEmail_OnlyWhenModeratorChanges()
    {
        var moderatorId = await CreatePreacherAsync("moderator@local");
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "moderator-token" });

        await RunAsRequesterAsync();
        var requestId = await CreateRequestAsync();
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await RunAsAdminAsync();
        await SendAsync(new AssignMeetingRequestCommand { Id = requestId, ModeratorId = moderatorId });
        await SendAsync(new AssignMeetingRequestCommand { Id = requestId, ModeratorId = moderatorId }); // unchanged: no second job
        await RunEnqueuedJobsAsync();

        var notification = (await NotificationsForAsync(moderatorId)).Single();
        notification.NotificationType.ShouldBe("MeetingRequest");
        notification.Title.ShouldBe("New meeting request assigned to you");
        notification.ActionUrl!.ShouldStartWith($"meeting-requests/{requestId}?event=assigned");
        ShouldHavePushedTo(new[] { "moderator-token" });
        ShouldHaveEmailed("moderator@local", "Meeting request assigned to you");
    }

    [Test]
    public async Task Accept_ByUnassignedModerator_NotifiesRequester_AndNamesModerator()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "phone-token" });
        var requestId = await CreateRequestAsync();
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await CreatePreacherAsync("moderator@local");
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = requestId, Status = MeetingInquiryRequestStatus.Accepted });
        await RunEnqueuedJobsAsync();

        (await NotificationsForAsync(requesterId)).Single().Title.ShouldBe("Your meeting request was accepted");
        ShouldHavePushedTo(new[] { "phone-token" });
        ShouldHaveEmailed(RequesterEmail, "Your meeting request was accepted");

        // The email links to the request's page on the website.
        var detailsUrl = $"{MeetingRequestNotificationJobs.DefaultWebBaseUrl}/question-and-answer/meet-scholar/history/{requestId}";
        EmailService.Verify(e => e.SendEmailAsync(RequesterEmail, It.IsAny<string>(),
            It.Is<string>(body => body.Contains(detailsUrl)), true), Times.Once());
    }

    [Test]
    public async Task Emails_ToRequester_LinkToTheDetailsPage_AndStaffEmailsDoNot()
    {
        await RunAsRequesterAsync();
        var requestId = await CreateRequestAsync();
        await RunEnqueuedJobsAsync();

        var detailsUrl = $"/question-and-answer/meet-scholar/history/{requestId}";
        EmailService.Verify(e => e.SendEmailAsync(RequesterEmail, It.Is<string>(s => s.Contains("We received")),
            It.Is<string>(body => body.Contains(detailsUrl)), true), Times.Once());
        EmailService.Verify(e => e.SendEmailAsync(InboxEmail, It.IsAny<string>(),
            It.Is<string>(body => body.Contains(detailsUrl)), true), Times.Never());

        // The email is in UTC, so it says so and points to the page for the reader's local time.
        EmailService.Verify(e => e.SendEmailAsync(RequesterEmail, It.IsAny<string>(),
            It.Is<string>(body => body.Contains("UTC") && body.Contains("shows the time in your local timezone")), true), Times.Once());
    }

    [Test]
    public async Task Reject_NotifiesRequester_AndSettingTheSameStatusAgainDoesNotNotify()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        var requestId = await CreateRequestAsync();
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await RunAsAdminAsync();
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = requestId, Status = MeetingInquiryRequestStatus.Rejected });
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = requestId, Status = MeetingInquiryRequestStatus.Rejected });
        await RunEnqueuedJobsAsync();

        (await NotificationsForAsync(requesterId)).Single().Title.ShouldBe("Your meeting request was declined");
        ShouldHaveEmailed(RequesterEmail, "declined");
    }

    [Test]
    public async Task Cancel_ByRequester_NotifiesAssignedModerator_WithoutEmail()
    {
        var moderatorId = await CreatePreacherAsync("moderator@local");
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "moderator-token" });

        await RunAsRequesterAsync();
        var requestId = await CreateRequestAsync();
        await RunAsAdminAsync();
        await SendAsync(new AssignMeetingRequestCommand { Id = requestId, ModeratorId = moderatorId });
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await RunAsRequesterAsync();
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = requestId, Status = MeetingInquiryRequestStatus.Cancelled });
        await RunEnqueuedJobsAsync();

        (await NotificationsForAsync(moderatorId)).Last().Title.ShouldBe("Meeting request cancelled");
        ShouldHavePushedTo(new[] { "moderator-token" });
        EmailService.Invocations.ShouldBeEmpty();
    }

    [Test]
    public async Task Pending_DoesNotNotifyAnyone()
    {
        await RunAsRequesterAsync();
        var requestId = await CreateRequestAsync();
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await RunAsAdminAsync();
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = requestId, Status = MeetingInquiryRequestStatus.Rejected });
        ResetFakes();
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = requestId, Status = MeetingInquiryRequestStatus.Pending });
        await RunEnqueuedJobsAsync();

        EmailService.Invocations.ShouldBeEmpty();
        FirebaseMessaging.Invocations.ShouldBeEmpty();
    }

    [Test]
    public async Task Response_NotifiesRequester_WithResponseTextEncoded()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        var requestId = await CreateRequestAsync();
        await RunEnqueuedJobsAsync();
        ResetFakes();

        // The response endpoint is role-gated, so the mocked user needs the Administrator role claim.
        await RunAsUserAsync("admin@local", Password, UserRole.Admin, new[] { Roles.Administrator });
        await SendAsync(new UpdateMeetingRequestResponseCommand { Id = requestId, Response = "See you <i>there</i>." });
        await RunEnqueuedJobsAsync();

        (await NotificationsForAsync(requesterId)).Single().Title.ShouldBe("New response to your meeting request");
        EmailService.Verify(e => e.SendEmailAsync(RequesterEmail, It.IsAny<string>(),
            It.Is<string>(body => body.Contains("&lt;i&gt;there&lt;/i&gt;")), true), Times.Once());
    }

    [Test]
    public async Task RetriedDispatch_DoesNotNotifyTwice()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "phone-token" });
        var requestId = await CreateRequestAsync();
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await RunAsAdminAsync();
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = requestId, Status = MeetingInquiryRequestStatus.Rejected });
        var dispatch = EnqueuedJobs.Single();

        await RunJobAsync(dispatch);
        await RunJobAsync(dispatch); // Hangfire retry of the same job

        (await NotificationsForAsync(requesterId)).Count.ShouldBe(1);
        ShouldHavePushedTo(new[] { "phone-token" });
    }
}
