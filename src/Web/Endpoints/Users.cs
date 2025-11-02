using AskAMuslimBackend.Infrastructure.Identity;

namespace AskAMuslimBackend.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapIdentityApi<ApplicationUser>();
    }
}
