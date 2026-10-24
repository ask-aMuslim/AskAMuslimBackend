using AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestStatus;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetAllMeetingRequests;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestCounts;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.FunctionalTests.MeetingRequests;

using static Testing;

public class MeetingRequestListTests : BaseTestFixture
{
    private const string Password = "Testing1234!";

    private static async Task<Guid> CreateRequestAsync(string message, DateTime scheduledAt)
    {
        var result = await SendAsync(new CreateMeetingRequestCommand
        {
            Topic = MeetingInquiryTopic.Fiqh,
            Message = message,
            Languages = new List<Language> { Language.English },
            ScheduledAt = scheduledAt
        });
        result.Succeeded.ShouldBeTrue(string.Join(", ", result.Errors));
        return result.Data;
    }

    [Test]
    public async Task List_IncludesTheRequesterEmail_AndCanSearchByIt()
    {
        await RunAsUserAsync("alice.requester@local", Password, UserRole.Student);
        var aliceRequest = await CreateRequestAsync("First question", DateTime.UtcNow.AddDays(3));
        await RunAsUserAsync("bob@local", Password, UserRole.Student);
        await CreateRequestAsync("Second question", DateTime.UtcNow.AddDays(3));

        await RunAsUserAsync("admin@local", Password, UserRole.Admin);
        var result = await SendAsync(new GetAllMeetingRequestsQuery { SearchTerm = "ALICE.requester" });

        var item = result.Data!.Items.ShouldHaveSingleItem();
        item.Id.ShouldBe(aliceRequest);
        item.RequesterEmail.ShouldBe("alice.requester@local");
    }

    [Test]
    public async Task List_CanFilterByTheScheduledDateRange()
    {
        await RunAsUserAsync("owner@local", Password, UserRole.Student);
        await CreateRequestAsync("Soon", DateTime.UtcNow.AddDays(2));
        var later = await CreateRequestAsync("Later", DateTime.UtcNow.AddDays(10));

        await RunAsUserAsync("admin@local", Password, UserRole.Admin);
        var result = await SendAsync(new GetAllMeetingRequestsQuery { From = DateTime.UtcNow.AddDays(5) });
        result.Data!.Items.Select(r => r.Id).ShouldBe(new[] { later });

        var none = await SendAsync(new GetAllMeetingRequestsQuery
        {
            From = DateTime.UtcNow.AddDays(4),
            To = DateTime.UtcNow.AddDays(6)
        });
        none.Data!.Items.ShouldBeEmpty();
    }

    [Test]
    public async Task Counts_CountPendingAndUnassignedRequests()
    {
        await RunAsUserAsync("owner@local", Password, UserRole.Student);
        await CreateRequestAsync("One", DateTime.UtcNow.AddDays(3));
        var rejected = await CreateRequestAsync("Two", DateTime.UtcNow.AddDays(3));

        await RunAsUserAsync("admin@local", Password, UserRole.Admin);
        await SendAsync(new UpdateMeetingRequestStatusCommand
        {
            Id = rejected,
            Status = MeetingInquiryRequestStatus.Rejected
        });

        var counts = (await SendAsync(new GetMeetingRequestCountsQuery())).Data!;

        counts.Pending.ShouldBe(1);
        // The rejected request no longer needs a moderator
        counts.Unassigned.ShouldBe(1);
    }
}
