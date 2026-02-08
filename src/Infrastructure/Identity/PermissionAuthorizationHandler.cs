using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace AskAMuslimBackend.Infrastructure.Identity;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        bool hasPermission;

        if (requirement.RequireAll)
        {
            hasPermission = await permissionService.HasAllPermissionsAsync(userId, requirement.RequiredPermissions);
        }
        else
        {
            hasPermission = await permissionService.HasAnyPermissionAsync(userId, requirement.RequiredPermissions);
        }

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
