using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToSupportMail;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailById;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMails;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

/// <summary>
/// Support mails assigned to the current moderator. Handlers enforce the assignment.
/// </summary>
public class SupportMailsModerator : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAssigned)
            .RequireAuthorization()
            .WithName("GetAssignedSupportMails")
            .WithSummary("Get support mails assigned to the current moderator");
        groupBuilder.MapGet(GetById, "{id:guid}")
            .RequireAuthorization()
            .WithName("GetAssignedSupportMail")
            .WithSummary("Get an assigned support mail with its messages");
        groupBuilder.MapPost(Reply, "{id:guid}/replies")
            .RequireAuthorization()
            .WithName("ReplyToAssignedSupportMail")
            .WithSummary("Reply to an assigned support mail");
    }

    public async Task<IResult> GetAssigned(ISender sender, [AsParameters] GetSupportMailsQuery query)
    {
        var result = await sender.Send(query with { AssignedToMe = true, ModeratorId = null });
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
}
