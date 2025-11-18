using AskAMuslimBackend.Application.InquiryRequests.Commands.CreateInquiryRequest;
using AskAMuslimBackend.Application.InquiryRequests.Commands.UpdateInquiryRequestStatus;
using AskAMuslimBackend.Application.InquiryRequests.Commands.UpdateInquiryRequestResponse;
using AskAMuslimBackend.Application.InquiryRequests.Queries;
using AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestById;
using AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByRequester;
using AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByScholar;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class InquiryRequests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetById, "{id}").WithName("GetInquiryRequest").WithSummary("Get inquiry request by id");
        groupBuilder.MapGet(GetByRequester, "by-requester/{requesterId}").RequireAuthorization().WithName("GetInquiryRequestsByRequester").WithSummary("Get inquiry requests for requester");
        groupBuilder.MapGet(GetByScholar, "by-scholar/{scholarId}").RequireAuthorization().WithName("GetInquiryRequestsByScholar").WithSummary("Get inquiry requests for scholar");
        groupBuilder.MapPost(Create).RequireAuthorization().WithName("CreateInquiryRequest").WithSummary("Create inquiry request");
        groupBuilder.MapPut(UpdateStatus, "{id}/status").RequireAuthorization().WithName("UpdateInquiryRequestStatus").WithSummary("Update inquiry request status");
        groupBuilder.MapPut(UpdateResponse, "{id}/response").RequireAuthorization().WithName("UpdateInquiryRequestResponse").WithSummary("Set admin response on inquiry request");
    }

    public async Task<Results<Ok<InquiryRequestDto>, NotFound>> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetInquiryRequestByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    public async Task<Ok<List<InquiryRequestDto>>> GetByRequester(ISender sender, Guid requesterId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetInquiryRequestsByRequesterQuery(requesterId, status));
        return TypedResults.Ok(result);
    }

    public async Task<Ok<List<InquiryRequestDto>>> GetByScholar(ISender sender, Guid scholarId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetInquiryRequestsByScholarQuery(scholarId, status));
        return TypedResults.Ok(result);
    }

    public async Task<Created<Guid>> Create(ISender sender, CreateInquiryRequestCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/{nameof(InquiryRequests)}/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateStatus(ISender sender, Guid id, UpdateInquiryRequestStatusCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, BadRequest>> UpdateResponse(ISender sender, Guid id, UpdateInquiryRequestResponseCommand command)
    {
        if (id != command.Id) return TypedResults.BadRequest();
        await sender.Send(command);
        return TypedResults.NoContent();
    }
}
