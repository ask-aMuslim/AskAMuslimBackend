using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Commands.UpdateUserPermissions;

[Authorize(Policy = Policies.ManageRolesAndPermissions)]
public record UpdateUserPermissionsCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public Permission? Permissions { get; init; }  // Optional: for backward compatibility
    public List<string>? PermissionNames { get; init; }  // New: accept permission names as strings
    public string? Notes { get; init; }
}

public class UpdateUserPermissionsCommandValidator : AbstractValidator<UpdateUserPermissionsCommand>
{
    public UpdateUserPermissionsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x)
            .Must(x => x.Permissions.HasValue || (x.PermissionNames != null && x.PermissionNames.Any()))
            .WithMessage("Either Permissions or PermissionNames must be provided");
    }
}

public class UpdateUserPermissionsCommandHandler : IRequestHandler<UpdateUserPermissionsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateUserPermissionsCommandHandler(
        IApplicationDbContext _context,
        IUser currentUser)
    {
        this._context = _context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateUserPermissionsCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
        {
            return Result.Failure(new[] { "Invalid current user ID" });
        }

        // Check if user exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return Result.Failure(new[] { "User not found" });
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
        else if (request.Permissions.HasValue)
        {
            permissions = request.Permissions.Value;
        }
        else
        {
            return Result.Failure(new[] { "Either Permissions or PermissionNames must be provided" });
        }

        // Get or create user permission
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == request.UserId, cancellationToken);

        if (userPermission == null)
        {
            userPermission = new UserPermission
            {
                UserId = request.UserId,
                Permissions = permissions,
                GrantedDate = DateTime.UtcNow,
                GrantedByUserId = currentUserId,
                Notes = request.Notes
            };
            _context.UserPermissions.Add(userPermission);
        }
        else
        {
            userPermission.Permissions = permissions;
            userPermission.GrantedDate = DateTime.UtcNow;
            userPermission.GrantedByUserId = currentUserId;
            userPermission.Notes = request.Notes;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
