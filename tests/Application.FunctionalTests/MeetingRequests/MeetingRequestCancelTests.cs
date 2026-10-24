using AskAMuslimBackend.Application.MeetingRequests.Commands.AssignMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.CancelMyMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestStatus;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMyMeetingRequestById;
using AskAMuslimBackend.Application.Notifications.Commands.RegisterFcmToken;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.FunctionalTests.MeetingRequests;

using static Testing;

public class MeetingRequestCancelTests : BaseTestFixture
{
    private const string Password = "Testing1234!";

    private static Task<string> RunAsOwnerAsync() => RunAsUserAsync("owner@local", Password, UserRole.Student);
    private static Task<string> RunAsOtherAsync() => RunAsUserAsync("other@local", Password, UserRole.Student);
    private static Task<string> RunAsAdminAsync() => RunAsUserAsync("admin@local", Password, UserRole.Admin);

    private static async Task<Guid> CreateRequestAsync()
    {
        var result = await SendAsync(new CreateMeetingRequestCommand
        {
            Topic = MeetingInquiryTopic.Fiqh,
            Message = "A private question",
            Languages = new List<Language> { Language.English },
            ScheduledAt = DateTime.UtcNow.AddDays(3)
        });
        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        return result.Data;
    }

    private static async Task<MeetingInquiryRequestStatus> StatusOfAsync(Guid id) =>
        (await FindAsync<MeetingRequest>(id))!.Status;

    [Test]
    public async Task Cancel_PendingRequest_SetsCancelled()
    {
        await RunAsOwnerAsync();
        var id = await CreateRequestAsync();

        var result = await SendAsync(new CancelMyMeetingRequestCommand { Id = id });

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        (await StatusOfAsync(id)).ShouldBe(MeetingInquiryRequestStatus.Cancelled);
    }

    [Test]
    public async Task Cancel_AcceptedRequest_NotifiesTheModerator()
    {
        var moderatorId = Guid.Parse(await RunAsUserAsync("moderator@local", Password, UserRole.Preacher));
        await AddAsync(new Preacher { UserId = moderatorId });
        await SendAsync(new RegisterFcmTokenCommand { FcmToken = "moderator-token" });

        await RunAsOwnerAsync();
        var id = await CreateRequestAsync();
        await RunAsAdminAsync();
        await SendAsync(new AssignMeetingRequestCommand { Id = id, ModeratorId = moderatorId });
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = id, Status = MeetingInquiryRequestStatus.Accepted });
        await RunEnqueuedJobsAsync();
        ResetFakes();

        await RunAsOwnerAsync();
        var result = await SendAsync(new CancelMyMeetingRequestCommand { Id = id });
        await RunEnqueuedJobsAsync();

        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        (await StatusOfAsync(id)).ShouldBe(MeetingInquiryRequestStatus.Cancelled);
        FirebaseMessaging.Verify(f => f.SendToTokensAsync(
            It.Is<IEnumerable<string>>(t => t.Single() == "moderator-token"),
            It.Is<AskAMuslimBackend.Application.Notifications.Common.Models.PushNotificationRequest>(p => p.Title == "Meeting request cancelled"),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Test]
    public async Task Cancel_AnotherUsersRequest_IsNotFound_AndChangesNothing()
    {
        await RunAsOwnerAsync();
        var id = await CreateRequestAsync();

        await RunAsOtherAsync();
        var result = await SendAsync(new CancelMyMeetingRequestCommand { Id = id });

        result.Succeeded.ShouldBeFalse();
        result.Errors.ShouldContain("Meeting request not found");
        (await StatusOfAsync(id)).ShouldBe(MeetingInquiryRequestStatus.Pending);
    }

    [Test]
    [TestCase(MeetingInquiryRequestStatus.Rejected)]
    [TestCase(MeetingInquiryRequestStatus.Completed)]
    [TestCase(MeetingInquiryRequestStatus.Cancelled)]
    public async Task Cancel_FinishedRequest_IsRefused(MeetingInquiryRequestStatus status)
    {
        await RunAsOwnerAsync();
        var id = await CreateRequestAsync();
        await RunAsAdminAsync();
        await SendAsync(new UpdateMeetingRequestStatusCommand { Id = id, Status = status });

        await RunAsOwnerAsync();
        var result = await SendAsync(new CancelMyMeetingRequestCommand { Id = id });

        result.Succeeded.ShouldBeFalse();
        (await StatusOfAsync(id)).ShouldBe(status);
    }

    [Test]
    public async Task GetMine_ReturnsOwnRequest_ButNotSomeoneElses()
    {
        await RunAsOwnerAsync();
        var id = await CreateRequestAsync();

        var own = await SendAsync(new GetMyMeetingRequestByIdQuery(id));
        own.Succeeded.ShouldBeTrue();
        own.Data!.Id.ShouldBe(id);

        await RunAsOtherAsync();
        var other = await SendAsync(new GetMyMeetingRequestByIdQuery(id));
        other.Succeeded.ShouldBeFalse();
    }
}
