using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Newsletters.Commands.SendNewsletter;
using AskAMuslimBackend.Application.Newsletters.Commands.SubscribeToNewsletter;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Newsletters : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(Subscribe, "subscribe")
            .AllowAnonymous()
            .WithName("SubscribeToNewsletter")
            .WithSummary("Subscribe to the newsletter")
            .WithDescription("Adds an email address to the newsletter subscription list")
            .Produces<Result<Guid>>(StatusCodes.Status200OK)
            .Produces<Result<Guid>>(StatusCodes.Status400BadRequest);

        // Require SuperAdmin directly or proper permissions policy
        groupBuilder.MapPost(SendNewsletter, "send")
            .RequireAuthorization("RequireSuperAdmin")
            .WithName("SendNewsletter")
            .WithSummary("Send newsletter to subscribers")
            .WithDescription("Sends an HTML email template to all active newsletter subscribers")
            .Produces<Result>(StatusCodes.Status200OK)
            .Produces<Result>(StatusCodes.Status400BadRequest);
    }

    public async Task<IResult> Subscribe(
        ISender sender,
        SubscribeToNewsletterCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    public async Task<IResult> SendNewsletter(
        ISender sender,
        SendNewsletterCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}