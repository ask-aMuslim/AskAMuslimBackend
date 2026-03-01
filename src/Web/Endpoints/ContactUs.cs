using AskAMuslimBackend.Application.ContactUs.Commands;

namespace AskAMuslimBackend.Web.Endpoints;

public class ContactUs : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(SendMessage)
            .AllowAnonymous()
            .WithName("SendContactUsMessage")
            .WithSummary("Send a contact us message");
    }

    public async Task<IResult> SendMessage(ISender sender, SendContactUsMessageCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
