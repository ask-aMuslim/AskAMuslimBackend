using AskAMuslimBackend.Application.Notifications.Commands.MarkMyNotificationsAsRead;
using AskAMuslimBackend.Application.Notifications.Commands.RegisterFcmToken;
using AskAMuslimBackend.Application.Notifications.Commands.UnregisterFcmToken;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Application.Notifications.Queries.GetMyNotifications;
using AskAMuslimBackend.Application.SupportMails.Commands.AssignSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.CreateSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.MarkMySupportMailAsRead;
using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToMySupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.UpdateSupportMailStatus;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailCounts;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.FunctionalTests.SupportMails;

using static Testing;

public class SupportMailNotificationTests : BaseTestFixture
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

    private static async Task<Guid> CreateMailAsync()
    {
        var result = await SendAsync(new CreateSupportMailCommand
        {
            Topic = MeetingInquiryTopic.Fiqh,
            Subject = "Music Enquiry",
            Explanation = "Is music <b>allowed</b>?"
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
            It.Is<PushNotificationRequest>(p => p.CustomData != null && p.CustomData["type"] == "support_mail"),
            It.IsAny<CancellationToken>()), times ?? Times.Once());

    [Test]
    public async Task Create_QueuesJob_AndEmailsReceiptAndInbox_WithoutSendingInRequest()
    {
        await RunAsRequesterAsync();
        var mailId = await CreateMailAsync();

        // Nothing is sent inside the request, only queued.
        EmailService.Invocations.ShouldBeEmpty();
        var dispatch = EnqueuedJobs.Single();
        dispatch.Method.Name.ShouldBe(nameof(SupportMailNotificationJobs.DispatchAsync));
        dispatch.Args[0].ShouldBe(SupportMailNotificationEvent.Created);
        dispatch.Args[1].ShouldBe(mailId);

        await RunEnqueuedJobsAsync();

        ShouldHaveEmailed(RequesterEmail, "We received your inquiry");
        ShouldHaveEmailed(InboxEmail, "New support mail");
        EmailService.Verify(e => e.SendEmailAsync(InboxEmail, It.IsAny<string>(),
            It.Is<string>(body => body.Contains("&lt;b&gt;allowed&lt;/b&gt;")), true), Times.Once());
        FirebaseMessaging.Invocations.ShouldBeEmpty();
    }

    [Test]
    public async Task StaffReply_NotifiesRequesterOnEveryDevice_InAppPushAndEmail()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "phone-token", Platform = DevicePlatform.Android });
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "tablet-token", Platform = DevicePlatform.Ios });
        var mailId = await CreateMailAsync();
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await RunAsAdminAsync();
        await SendAsync(new ReplyToSupportMailCommand { Id = mailId, Body = "Thank you for your inquiry." });
        await RunEnqueuedJobsAsync();

        var notification = (await NotificationsForAsync(requesterId)).Single();
        notification.NotificationType.ShouldBe("SupportMail");
        notification.Title.ShouldBe("New reply on Mail #1");
        notification.ActionUrl!.ShouldStartWith($"support-mails/{mailId}?event=staff_reply");

        ShouldHavePushedTo(new[] { "phone-token", "tablet-token" });
        ShouldHaveEmailed(RequesterEmail, "New reply on your inquiry");

        // The requester sees it through the "my notifications" endpoints.
        await RunAsRequesterAsync();
        (await SendAsync(new GetMyUnreadNotificationsCountQuery())).Data.ShouldBe(1);
        (await SendAsync(new GetMyUnreadSupportMailsCountQuery())).Data.ShouldBe(1);
        var mine = await SendAsync(new GetMyNotificationsQuery { NotificationType = "SupportMail" });
        mine.Data!.Items.Single().Id.ShouldBe(notification.Id);

        await SendAsync(new MarkMyNotificationAsReadCommand(notification.Id));
        await SendAsync(new MarkMySupportMailAsReadCommand(mailId));
        (await SendAsync(new GetMyUnreadNotificationsCountQuery())).Data.ShouldBe(0);
        (await SendAsync(new GetMyUnreadSupportMailsCountQuery())).Data.ShouldBe(0);
    }

    [Test]
    public async Task RequesterReply_GoesToInbox_WhenUnassigned_AndToModerator_WhenAssigned()
    {
        var moderatorId = await CreatePreacherAsync("moderator@local");
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "moderator-token" });

        await RunAsRequesterAsync();
        var mailId = await CreateMailAsync();
        await SendAsync(new ReplyToMySupportMailCommand { Id = mailId, Body = "Any update?" });
        ResetFakes();
        await SendAsync(new ReplyToMySupportMailCommand { Id = mailId, Body = "Still waiting." });
        await RunEnqueuedJobsAsync();

        ShouldHaveEmailed(InboxEmail, "New reply on support mail #1");
        (await NotificationsForAsync(moderatorId)).ShouldBeEmpty();

        await RunAsAdminAsync();
        await SendAsync(new AssignSupportMailCommand { Id = mailId, ModeratorId = moderatorId });
        await RunEnqueuedJobsAsync();

        (await NotificationsForAsync(moderatorId)).Single().Title.ShouldBe("New inquiry assigned to you");
        ShouldHaveEmailed("moderator@local", "Support mail assigned to you");
        ShouldHavePushedTo(new[] { "moderator-token" });
        ResetFakes();

        await RunAsRequesterAsync();
        await SendAsync(new ReplyToMySupportMailCommand { Id = mailId, Body = "Thanks, waiting for you." });
        await RunEnqueuedJobsAsync();

        (await NotificationsForAsync(moderatorId)).Count.ShouldBe(2);
        ShouldHaveEmailed("moderator@local", "New reply on support mail #1");
        ShouldHaveEmailed(InboxEmail, "New reply", Times.Never());
    }

    [Test]
    public async Task Close_NotifiesRequesterInAppAndPush_WithoutEmail()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "phone-token" });
        var mailId = await CreateMailAsync();
        ResetFakes();

        await RunAsAdminAsync();
        await SendAsync(new CloseSupportMailCommand(mailId));
        await SendAsync(new CloseSupportMailCommand(mailId)); // already closed: no second notification
        await RunEnqueuedJobsAsync();

        (await NotificationsForAsync(requesterId)).Single().Title.ShouldBe("Mail #1 was closed");
        ShouldHavePushedTo(new[] { "phone-token" });
        EmailService.Invocations.ShouldBeEmpty();
    }

    [Test]
    public async Task RetriedDispatch_DoesNotNotifyTwice()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "phone-token" });
        var mailId = await CreateMailAsync();
        ResetFakes();

        await RunAsAdminAsync();
        await SendAsync(new ReplyToSupportMailCommand { Id = mailId, Body = "Answer" });
        var dispatch = EnqueuedJobs.Single();

        await RunJobAsync(dispatch);
        await RunJobAsync(dispatch); // Hangfire retry of the same job

        (await NotificationsForAsync(requesterId)).Count.ShouldBe(1);
        ShouldHavePushedTo(new[] { "phone-token" });
    }

    [Test]
    public async Task InvalidDeviceTokens_AreRemoved()
    {
        var requesterId = Guid.Parse(await RunAsRequesterAsync());
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "dead-token" });
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "live-token" });
        var mailId = await CreateMailAsync();
        ResetFakes();
        FirebaseMessaging
            .Setup(f => f.SendToTokensAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<PushNotificationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PushNotificationResult { SuccessCount = 1, FailureCount = 1, InvalidTokens = new() { "dead-token" } });

        await RunAsAdminAsync();
        await SendAsync(new ReplyToSupportMailCommand { Id = mailId, Body = "Answer" });
        await RunEnqueuedJobsAsync();

        var tokens = new List<string>();
        await ExecuteDbContextAsync(async context =>
            tokens.AddRange(await context.UserDeviceTokens.Where(t => t.UserId == requesterId).Select(t => t.Token).ToListAsync()));
        tokens.ShouldBe(new[] { "live-token" });
    }

    [Test]
    public async Task DeviceTokens_MoveBetweenAccounts_AndUnregisterOnLogout()
    {
        var firstUserId = Guid.Parse(await RunAsRequesterAsync());
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "shared-device", Platform = DevicePlatform.Android });

        // Someone else logs in on the same phone
        var secondUserId = Guid.Parse(await RunAsUserAsync("second@local", Password, UserRole.Student));
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "shared-device" });

        var owner = Guid.Empty;
        await ExecuteDbContextAsync(async context =>
            owner = (await context.UserDeviceTokens.SingleAsync(t => t.Token == "shared-device")).UserId);
        owner.ShouldBe(secondUserId);
        owner.ShouldNotBe(firstUserId);

        await SendAsync(new UnregisterFcmTokenCommand { FcmToken = "shared-device" });
        (await CountAsync<UserDeviceToken>()).ShouldBe(0);
        (await FindAsync<User>(secondUserId))!.FcmToken.ShouldBeNull();
    }

    [Test]
    public async Task Counts_ReflectAwaitingAndUnassignedMails()
    {
        var moderatorId = await CreatePreacherAsync("moderator@local");

        await RunAsRequesterAsync();
        var firstMailId = await CreateMailAsync();
        await ExecuteDbContextAsync(context => context.SupportMails
            .Where(m => m.Id == firstMailId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Created, m => m.Created - TimeSpan.FromHours(2))));
        await CreateMailAsync();

        await RunAsAdminAsync();
        await SendAsync(new AssignSupportMailCommand { Id = firstMailId, ModeratorId = moderatorId });

        var adminCounts = (await SendAsync(new GetSupportMailCountsQuery())).Data!;
        adminCounts.AwaitingResponse.ShouldBe(2);
        adminCounts.Unassigned.ShouldBe(1);

        await RunAsUserAsync("moderator@local", Password, UserRole.Preacher);
        var moderatorCounts = (await SendAsync(new GetSupportMailCountsQuery(AssignedToMe: true))).Data!;
        moderatorCounts.AwaitingResponse.ShouldBe(1);
        moderatorCounts.Unassigned.ShouldBe(0);
        moderatorCounts.AssignedToMeAwaiting.ShouldBe(1);
    }
}
