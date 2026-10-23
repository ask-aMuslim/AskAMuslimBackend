using AskAMuslimBackend.Application.Common.Exceptions;
using AskAMuslimBackend.Application.MeetingRequests.Commands.AssignMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByModerator;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByRequester;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.FunctionalTests.MeetingRequests;

using static Testing;

public class MeetingRequestAccessTests : BaseTestFixture
{
    private const string Password = "Testing1234!";

    private static Task<string> RunAsStudentAsync(string email) => RunAsUserAsync(email, Password, UserRole.Student);

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

    [Test]
    public async Task ByRequester_ReturnsOwnRequests()
    {
        var ownerId = Guid.Parse(await RunAsStudentAsync("owner@local"));
        var requestId = await CreateRequestAsync();

        var result = await SendAsync(new GetMeetingRequestsByRequesterQuery(ownerId));

        result.Data!.Select(r => r.Id).ShouldBe(new[] { requestId });
    }

    [Test]
    public async Task ByRequester_RejectsAnotherUser()
    {
        var ownerId = Guid.Parse(await RunAsStudentAsync("owner@local"));
        await CreateRequestAsync();

        await RunAsStudentAsync("other@local");

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetMeetingRequestsByRequesterQuery(ownerId)));
    }

    [Test]
    public async Task ByRequester_AllowsAdminWithViewMeetingRequestsPermission()
    {
        var ownerId = Guid.Parse(await RunAsStudentAsync("owner@local"));
        var requestId = await CreateRequestAsync();

        await RunAsUserAsync("admin@local", Password, UserRole.Admin);
        var result = await SendAsync(new GetMeetingRequestsByRequesterQuery(ownerId));

        result.Data!.Select(r => r.Id).ShouldBe(new[] { requestId });
    }

    [Test]
    public async Task ByModerator_AllowsTheModeratorThemself_ButNotAnotherUser()
    {
        var moderatorId = Guid.Parse(await RunAsUserAsync("moderator@local", Password, UserRole.Preacher));
        await AddAsync(new Preacher { UserId = moderatorId });

        await RunAsStudentAsync("owner@local");
        var requestId = await CreateRequestAsync();

        await RunAsUserAsync("admin@local", Password, UserRole.Admin);
        await SendAsync(new AssignMeetingRequestCommand { Id = requestId, ModeratorId = moderatorId });

        await RunAsUserAsync("moderator@local", Password, UserRole.Preacher);
        var own = await SendAsync(new GetMeetingRequestsByModeratorQuery(moderatorId));
        own.Data!.Select(r => r.Id).ShouldBe(new[] { requestId });

        await RunAsStudentAsync("other@local");
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            SendAsync(new GetMeetingRequestsByModeratorQuery(moderatorId)));
    }
}
