using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AskAMuslimBackend.Infrastructure.Identity;

public class PermissionRequirement : IAuthorizationRequirement
{
    public Permission[] RequiredPermissions { get; }
    public bool RequireAll { get; }

    public PermissionRequirement(Permission[] requiredPermissions, bool requireAll = false)
    {
        RequiredPermissions = requiredPermissions;
        RequireAll = requireAll;
    }

    public PermissionRequirement(Permission requiredPermission)
    {
        RequiredPermissions = new[] { requiredPermission };
        RequireAll = false;
    }
}
