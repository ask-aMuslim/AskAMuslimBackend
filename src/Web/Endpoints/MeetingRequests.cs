using AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestStatus;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestResponse;
using AskAMuslimBackend.Application.MeetingRequests.Commands.AssignMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Queries;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetAllMeetingRequests;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestById;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByRequester;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByModerator;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class MeetingRequests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAll)
            .RequireAuthorization(Policies.ViewMeetingRequests)
            .WithName("GetAllMeetingRequests")
            .WithSummary("Get all meeting requests");
        groupBuilder.MapGet(GetById, "{id}")
            .RequireAuthorization(Policies.ViewMeetingRequests)
            .WithName("GetMeetingRequest")
            .WithSummary("Get meeting request by id");
        groupBuilder.MapGet(GetByRequester, "by-requester/{requesterId}")
            .RequireAuthorization()
            .WithName("GetMeetingRequestsByRequester")
            .WithSummary("Get meeting requests for requester");
        groupBuilder.MapGet(GetByModerator, "by-moderator/{moderatorId}")
            .RequireAuthorization()
            .WithName("GetMeetingRequestsByModerator")
            .WithSummary("Get meeting requests for moderator");
        groupBuilder.MapPost(Create)
            .RequireAuthorization()
            .WithName("CreateMeetingRequest")
            .WithSummary("Create meeting request");
        groupBuilder.MapPut(UpdateStatus, "{id}/status")
            .RequireAuthorization(Policies.ManageMeetingRequests)
            .WithName("UpdateMeetingRequestStatus")
            .WithSummary("Update meeting request status");
        groupBuilder.MapPut(UpdateResponse, "{id}/response")
            .RequireAuthorization(Policies.ManageMeetingRequests)
            .WithName("UpdateMeetingRequestResponse")
            .WithSummary("Set admin response on meeting request");
        groupBuilder.MapPut(Assign, "{id}/assign")
            .RequireAuthorization(Policies.ManageMeetingRequests)
            .WithName("AssignMeetingRequest")
            .WithSummary("Assign a moderator to a meeting request");
    }

    public async Task<IResult> GetAll(ISender sender, [AsParameters] GetAllMeetingRequestsQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetMeetingRequestByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByRequester(ISender sender, Guid requesterId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetMeetingRequestsByRequesterQuery(requesterId, status));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByModerator(ISender sender, Guid moderatorId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetMeetingRequestsByModeratorQuery(moderatorId, status));
        return result.ToHttpResult();
    }

    public async Task<IResult> Create(ISender sender, CreateMeetingRequestCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(MeetingRequests)}/{result.Data}");
    }

    public async Task<IResult> UpdateStatus(ISender sender, Guid id, UpdateMeetingRequestStatusCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateResponse(ISender sender, Guid id, UpdateMeetingRequestResponseCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> Assign(ISender sender, Guid id, AssignMeetingRequestCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }
}
