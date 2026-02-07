using AskAMuslimBackend.Application.Roles.Queries.GetRoles;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Roles : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAll).WithName("GetRoles").WithSummary("Get all available user roles");
    }

    public async Task<IResult> GetAll(ISender sender)
    {
        var result = await sender.Send(new GetRolesQuery());
        return result.ToHttpResult();
    }
}
