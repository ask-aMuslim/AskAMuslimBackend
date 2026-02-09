using AskAMuslimBackend.Application.Permissions;
using AskAMuslimBackend.Application.Permissions.Commands.UpdateRolePermissions;
using AskAMuslimBackend.Application.Permissions.Commands.UpdateUserPermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetAllPermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetMyPermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetRolePermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetUserPermissions;
using AskAMuslimBackend.Application.Permissions.Queries.GetUsersForPermissionAssignment;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Permissions : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMyPermissions, "my")
            .RequireAuthorization()
            .WithName("GetMyPermissions")
            .WithSummary("Get permissions for the current authenticated user");

        groupBuilder.MapGet(GetAllPermissions)
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("GetAllPermissions")
            .WithSummary("Get all available permissions grouped by category");

        groupBuilder.MapGet(GetUsersForPermissionAssignment, "users")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("GetUsersForPermissionAssignment")
            .WithSummary("Get all users (Admins, Instructors, Preachers) who can be assigned custom permissions with pagination and search");

        groupBuilder.MapGet(GetUserPermissions, "{userId}")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("GetUserPermissions")
            .WithSummary("Get permissions for a specific user");

        groupBuilder.MapPut(UpdateUserPermissions, "{userId}")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("UpdateUserPermissions")
            .WithSummary("Update permissions for a specific user (including individual Instructors and Preachers)");

        groupBuilder.MapGet(GetRolePermissions, "roles/{role}")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("GetRolePermissions")
            .WithSummary("Get permissions for a specific role");

        groupBuilder.MapPut(UpdateRolePermissions, "roles/{role}")
            .RequireAuthorization(Policies.ManageRolesAndPermissions)
            .WithName("UpdateRolePermissions")
            .WithSummary("Update default permissions for a specific role");
    }

    public async Task<IResult> GetMyPermissions(ISender sender)
    {
        var result = await sender.Send(new GetMyPermissionsQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetAllPermissions(ISender sender)
    {
        var result = await sender.Send(new GetAllPermissionsQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetUsersForPermissionAssignment(ISender sender, [AsParameters] GetUsersForPermissionAssignmentQuery query)
    {
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    public async Task<IResult> GetUserPermissions(ISender sender, Guid userId)
    {
        var result = await sender.Send(new GetUserPermissionsQuery(userId));
        return result.ToHttpResult();
    }

    public async Task<IResult> GetRolePermissions(ISender sender, UserRole role)
    {
        // Students and NonMuslims don't have role-based permissions
        if (role == UserRole.Student || role == UserRole.NonMuslim)
        {
            return Results.BadRequest(new { errors = new[] { "Cannot get permissions for Student or NonMuslim roles" } });
        }

        var result = await sender.Send(new GetRolePermissionsQuery(role));
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateRolePermissions(ISender sender, UserRole role, UpdateRolePermissionsCommand command)
    {
        // Students and NonMuslims don't have role-based permissions
        if (role == UserRole.Student || role == UserRole.NonMuslim)
        {
            return Results.BadRequest(new { errors = new[] { "Cannot update permissions for Student or NonMuslim roles" } });
        }

        var result = await sender.Send(command with { Role = role });
        return result.ToHttpResult();
    }

    public async Task<IResult> UpdateUserPermissions(ISender sender, Guid userId, UpdateUserPermissionsCommand command)
    {
        var result = await sender.Send(command with { UserId = userId });
        return result.ToHttpResult();
    }
}
