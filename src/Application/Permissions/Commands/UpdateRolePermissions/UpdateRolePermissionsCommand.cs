using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Commands.UpdateRolePermissions;

[Authorize(Policy = Policies.ManageRolesAndPermissions)]
public record UpdateRolePermissionsCommand : IRequest<Result>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public UserRole Role { get; init; }
    public List<string>? PermissionNames { get; init; }  // New: accept permission names as strings
    public string? Notes { get; init; }
}

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        // Role is set from route parameter, no validation needed here
    }
}

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IPermissionService _permissionService;

    public UpdateRolePermissionsCommandHandler(
        IApplicationDbContext context,
        IUser currentUser,
        IPermissionService permissionService)
    {
        _context = context;
        _currentUser = currentUser;
        _permissionService = permissionService;
    }

    public async Task<Result> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
        {
            return Result.Failure(new[] { "Invalid current user ID" });
        }

        // Convert permission names to Permission enum if provided
        Permission permissions;
        if (request.PermissionNames != null && request.PermissionNames.Any())
        {
            permissions = Permission.None;
            foreach (var permissionName in request.PermissionNames)
            {
                if (Enum.TryParse<Permission>(permissionName, true, out var parsedPermission))
                {
                    permissions |= parsedPermission;
                }
                else
                {
                    return Result.Failure(new[] { $"Invalid permission name: {permissionName}" });
                }
            }
        }
        else
        {
            return Result.Failure(new[] { "Either Permissions or PermissionNames must be provided" });
        }

        // Get or create role permission
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == request.Role, cancellationToken);

        if (rolePermission == null)
        {
            rolePermission = new RolePermission
            {
                Role = request.Role,
                Permissions = permissions,
                LastModifiedDate = DateTime.UtcNow,
                ModifiedByUserId = currentUserId,
                Notes = request.Notes
            };
            _context.RolePermissions.Add(rolePermission);
        }
        else
        {
            rolePermission.Permissions = permissions;
            rolePermission.LastModifiedDate = DateTime.UtcNow;
            rolePermission.ModifiedByUserId = currentUserId;
            rolePermission.Notes = request.Notes;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Update permissions for all users with this role
        var usersWithRole = await _context.Users
            .Where(u => u.Role == request.Role)
            .ToListAsync(cancellationToken);

        foreach (var user in usersWithRole)
        {
            // Check if user is SuperAdmin - SKIP them, they keep AllPermissions
            var isSuperAdmin = false;
            if (request.Role == UserRole.Admin)
            {
                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a => a.UserId == user.Id, cancellationToken);
                isSuperAdmin = admin?.IsSuperAdmin ?? false;
            }

            // Skip SuperAdmins - they always keep AllPermissions
            if (isSuperAdmin)
            {
                continue;
            }

            // Update regular users with new role permissions
            var userPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == user.Id, cancellationToken);

            if (userPermission == null)
            {
                userPermission = new UserPermission
                {
                    UserId = user.Id,
                    Permissions = permissions,
                    GrantedByUserId = currentUserId,
                    GrantedDate = DateTime.UtcNow,
                    Notes = $"Auto-synced from role {request.Role} permissions update"
                };
                _context.UserPermissions.Add(userPermission);
            }
            else
            {
                userPermission.Permissions = permissions;
                userPermission.GrantedByUserId = currentUserId;
                userPermission.GrantedDate = DateTime.UtcNow;
                userPermission.Notes = $"Auto-synced from role {request.Role} permissions update";
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache for this role so next request fetches fresh data
        _permissionService.InvalidateRolePermissionsCache(request.Role.ToString());

        return Result.Success();
    }
}
