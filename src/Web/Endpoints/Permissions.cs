using AskAMuslimBackend.Application.Permissions;
using AskAMuslimBackend.Application.Permissions.Commands.UpdateUserPermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetAllPermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetUserPermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetUsersForPermissionAssignment;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Permissions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAllPermissions)
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("GetAllPermissions")
            .WithSummary("Get all available permissions grouped by category");

        groupBuilder.MapGet(GetUsersForPermissionAssignment, "users")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("GetUsersForPermissionAssignment")
            .WithSummary("Get all users (Admins, Instructors, Preachers) who can be assigned custom permissions");

        groupBuilder.MapGet(GetUserPermissions, "{userId}")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("GetUserPermissions")
            .WithSummary("Get permissions for a specific user");

        groupBuilder.MapPut(UpdateUserPermissions, "{userId}")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("UpdateUserPermissions")
            .WithSummary("Update permissions for a specific user (including individual Instructors and Preachers)");
    }

    public async Task<IResult> GetAllPermissions(ISender sender)
    {
        var result = await sender.Send(new GetAllPermissionsQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetUsersForPermissionAssignment(ISender sender)
    {
        var result = await sender.Send(new GetUsersForPermissionAssignmentQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetUserPermissions(ISender sender, Guid userId)
    {
        var result = await sender.Send(new GetUserPermissionsQuery(userId));
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateUserPermissions(ISender sender, Guid userId, UpdateUserPermissionsCommand command)
    {
        if (userId != command.UserId)
        {
            return Results.BadRequest(new { errors = new[] { "User ID mismatch" } });
        }

        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
