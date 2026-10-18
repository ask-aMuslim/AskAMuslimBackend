using AskAMuslimBackend.Application.SupportMails.Commands.CreateSupportMail;
using AskAMuslimBackend.Application.SupportMails.Commands.MarkMySupportMailAsRead;
using AskAMuslimBackend.Application.SupportMails.Commands.ReplyToMySupportMail;
using AskAMuslimBackend.Application.SupportMails.Queries.GetMySupportMailById;
using AskAMuslimBackend.Application.SupportMails.Queries.GetMySupportMails;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailEligibility;
using AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailTopics;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

/// <summary>
/// Requester-facing support mails ("Contact Us" / "Mails History" in the mobile app).
/// Every handler scopes data to the current user.
/// </summary>
public class SupportMails : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetTopics, "topics")
            .RequireAuthorization()
            .WithName("GetSupportMailTopics")
            .WithSummary("Get inquiry types for a new support mail");
        groupBuilder.MapGet(GetEligibility, "eligibility")
            .RequireAuthorization()
            .WithName("GetSupportMailEligibility")
            .WithSummary("Check whether the current user can send a new support mail");
        groupBuilder.MapGet(GetMine)
            .RequireAuthorization()
            .WithName("GetMySupportMails")
            .WithSummary("Get the current user's support mails");
        groupBuilder.MapGet(GetMineById, "{id:guid}")
            .RequireAuthorization()
            .WithName("GetMySupportMail")
            .WithSummary("Get one of the current user's support mails with its messages");
        groupBuilder.MapPost(Create)
            .RequireAuthorization()
            .WithName("CreateSupportMail")
            .WithSummary("Send a new support mail");
        groupBuilder.MapPost(Reply, "{id:guid}/replies")
            .RequireAuthorization()
            .WithName("ReplyToMySupportMail")
            .WithSummary("Reply to one of the current user's support mails");
        groupBuilder.MapPut(MarkAsRead, "{id:guid}/read")
            .RequireAuthorization()
            .WithName("MarkMySupportMailAsRead")
            .WithSummary("Mark one of the current user's support mails as read");
    }

    public async Task<IResult> GetTopics(ISender sender)
    {
        var result = await sender.Send(new GetSupportMailTopicsQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetEligibility(ISender sender)
    {
        var result = await sender.Send(new GetSupportMailEligibilityQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetMine(ISender sender, [AsParameters] GetMySupportMailsQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetMineById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetMySupportMailByIdQuery(id));
        return result.ToHttpResult();
    }

    public async Task<IResult> Create(ISender sender, CreateSupportMailCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(SupportMails)}/{result.Data}");
    }

    public async Task<IResult> Reply(ISender sender, Guid id, ReplyToMySupportMailCommand command)
    {
        var result = await sender.Send(command with { Id = id });
        return result.ToHttpResult();
    }

    public async Task<IResult> MarkAsRead(ISender sender, Guid id)
    {
        var result = await sender.Send(new MarkMySupportMailAsReadCommand(id));
        return result.ToHttpResult();
    }
}
