using AskAMuslimBackend.Application.InquiryRequests.Commands.CreateInquiryRequest;
using AskAMuslimBackend.Application.InquiryRequests.Commands.UpdateInquiryRequestStatus;
using AskAMuslimBackend.Application.InquiryRequests.Commands.UpdateInquiryRequestResponse;
using AskAMuslimBackend.Application.InquiryRequests.Commands.AssignInquiryRequest;
using AskAMuslimBackend.Application.InquiryRequests.Queries;
using AskAMuslimBackend.Application.InquiryRequests.Queries.GetAllInquiryRequests;
using AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestById;
using AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByRequester;
using AskAMuslimBackend.Application.InquiryRequests.Queries.GetInquiryRequestsByModerator;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Web.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Web.Endpoints;

public class InquiryRequests : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAll)
            .RequireAuthorization(Policies.ViewInquiryRequests)
            .WithName("GetAllInquiryRequests")
            .WithSummary("Get all inquiry requests");
        groupBuilder.MapGet(GetById, "{id}")
            .RequireAuthorization(Policies.ViewInquiryRequests)
            .WithName("GetInquiryRequest")
            .WithSummary("Get inquiry request by id");
        groupBuilder.MapGet(GetByRequester, "by-requester/{requesterId}")
            .RequireAuthorization()
            .WithName("GetInquiryRequestsByRequester")
            .WithSummary("Get inquiry requests for requester");
        groupBuilder.MapGet(GetByModerator, "by-moderator/{moderatorId}")
            .RequireAuthorization()
            .WithName("GetInquiryRequestsByModerator")
            .WithSummary("Get inquiry requests for moderator");
        groupBuilder.MapPost(Create)
            .RequireAuthorization()
            .WithName("CreateInquiryRequest")
            .WithSummary("Create inquiry request");
        groupBuilder.MapPut(UpdateStatus, "{id}/status")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("UpdateInquiryRequestStatus")
            .WithSummary("Update inquiry request status");
        groupBuilder.MapPut(UpdateResponse, "{id}/response")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("UpdateInquiryRequestResponse")
            .WithSummary("Set admin response on inquiry request");
        groupBuilder.MapPut(Assign, "{id}/assign")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("AssignInquiryRequest")
            .WithSummary("Assign a moderator to an inquiry request");
    }

    public async Task<IResult> GetAll(ISender sender, [AsParameters] GetAllInquiryRequestsQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetInquiryRequestByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByRequester(ISender sender, Guid requesterId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetInquiryRequestsByRequesterQuery(requesterId, status));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetByModerator(ISender sender, Guid moderatorId, MeetingInquiryRequestStatus? status)
    {
        var result = await sender.Send(new GetInquiryRequestsByModeratorQuery(moderatorId, status));
        return result.ToHttpResult();
    }

    public async Task<IResult> Create(ISender sender, CreateInquiryRequestCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(InquiryRequests)}/{result.Data}");
    }

    public async Task<IResult> UpdateStatus(ISender sender, Guid id, UpdateInquiryRequestStatusCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateResponse(ISender sender, Guid id, UpdateInquiryRequestResponseCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> Assign(ISender sender, Guid id, AssignInquiryRequestCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }
}
