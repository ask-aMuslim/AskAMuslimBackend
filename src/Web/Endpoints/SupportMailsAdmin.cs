using AskAMuslimBackend.Application.SupportMails.Commands.AssignSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.UnassignSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.UpdateSupportMailStatus;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailById;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMails;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

/// <summary>
/// Admin dashboard management of support mails. Reuses the inquiry request permissions.
/// </summary>
public class SupportMailsAdmin : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAll)
            .RequireAuthorization(Policies.ViewInquiryRequests)
            .WithName("GetAllSupportMails")
            .WithSummary("Get all support mails");
        groupBuilder.MapGet(GetById, "{id:guid}")
            .RequireAuthorization(Policies.ViewInquiryRequests)
            .WithName("GetSupportMail")
            .WithSummary("Get a support mail with its messages");
        groupBuilder.MapPost(Reply, "{id:guid}/replies")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("ReplyToSupportMail")
            .WithSummary("Reply to a support mail as staff");
        groupBuilder.MapPut(Assign, "{id:guid}/assign")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("AssignSupportMail")
            .WithSummary("Assign a moderator to a support mail");
        groupBuilder.MapDelete(Unassign, "{id:guid}/assign")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("UnassignSupportMail")
            .WithSummary("Remove the assigned moderator from a support mail");
        groupBuilder.MapPut(Close, "{id:guid}/close")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("CloseSupportMail")
            .WithSummary("Close a support mail");
        groupBuilder.MapPut(Reopen, "{id:guid}/reopen")
            .RequireAuthorization(Policies.ManageInquiryRequests)
            .WithName("ReopenSupportMail")
            .WithSummary("Reopen a closed support mail");
    }

    public async Task<IResult> GetAll(ISender sender, [AsParameters] GetSupportMailsQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetSupportMailByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> Reply(ISender sender, Guid id, ReplyToSupportMailCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> Assign(ISender sender, Guid id, AssignSupportMailCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> Unassign(ISender sender, Guid id)
    {
        var result = await sender.Send(new UnassignSupportMailCommand(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> Close(ISender sender, Guid id)
    {
        var result = await sender.Send(new CloseSupportMailCommand(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> Reopen(ISender sender, Guid id)
    {
        var result = await sender.Send(new ReopenSupportMailCommand(id));
        return result.ToHttpResult();
    }
}
