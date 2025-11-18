using AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestStatus;
using AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestResponse;
using AskAMuslimBackend.Application.MeetingRequests.Queries;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestById;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByRequester;
using AskAMuslimBackend.Application.MeetingRequests.Queries.GetMeetingRequestsByScholar;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class MeetingRequests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetById, "{id}").WithName("GetMeetingRequest").WithSummary("Get meeting request by id");
        groupBuilder.MapGet(GetByRequester, "by-requester/{requesterId}").RequireAuthorization().WithName("GetMeetingRequestsByRequester").WithSummary("Get meeting requests for requester");
        groupBuilder.MapGet(GetByScholar, "by-scholar/{scholarId}").RequireAuthorization().WithName("GetMeetingRequestsByScholar").WithSummary("Get meeting requests for scholar");
        groupBuilder.MapPost(Create).RequireAuthorization().WithName("CreateMeetingRequest").WithSummary("Create meeting request");
        groupBuilder.MapPut(UpdateStatus, "{id}/status").RequireAuthorization().WithName("UpdateMeetingRequestStatus").WithSummary("Update meeting request status");
        groupBuilder.MapPut(UpdateResponse, "{id}/response").RequireAuthorization().WithName("UpdateMeetingRequestResponse").WithSummary("Set admin response on meeting request");
    }

    public async Task<Results<Ok<MeetingRequestDto>, NotFound>> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetMeetingRequestByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<MeetingRequestDto>>> GetByRequester(ISender sender, Guid requesterId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetMeetingRequestsByRequesterQuery(requesterId, status));
        return TypedResults.Ok(result);
    }

    public async Task<Ok<List<MeetingRequestDto>>> GetByScholar(ISender sender, Guid scholarId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetMeetingRequestsByScholarQuery(scholarId, status));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> Create(ISender sender, CreateMeetingRequestCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(MeetingRequests)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateStatus(ISender sender, Guid id, UpdateMeetingRequestStatusCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, BadRequest>> UpdateResponse(ISender sender, Guid id, UpdateMeetingRequestResponseCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }
}
