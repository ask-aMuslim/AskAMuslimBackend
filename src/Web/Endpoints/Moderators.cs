using AskAMuslimBackend.Application.Admins.Commands.CreateAdmin;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Moderators : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(AddModerator)
            .RequireAuthorization(Policies.ManageAdmins)
            .WithName("AddModerator")
            .WithSummary("Create a new admin, instructor, or preacher");
    }

    public async Task<IResult> AddModerator(ISender sender, CreateAdminCommand command)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
