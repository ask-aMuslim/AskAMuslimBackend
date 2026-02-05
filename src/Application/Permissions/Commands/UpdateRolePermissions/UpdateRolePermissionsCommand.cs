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
    public UserRole Role { get; init; }
    public Permission Permissions { get; init; }
    public string? Notes { get; init; }
}

public class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role");
    }
}

public class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public UpdateRolePermissionsCommandHandler(
        IApplicationDbContext context,
        IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUser.Id, out var currentUserId))
        {
            return Result.Failure(new[] { "Invalid current user ID" });
        }

        // Get or create role permission
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == request.Role, cancellationToken);

        if (rolePermission == null)
        {
            rolePermission = new RolePermission
            {
                Role = request.Role,
                Permissions = request.Permissions,
                LastModifiedDate = DateTime.UtcNow,
                ModifiedByUserId = currentUserId,
                Notes = request.Notes
            };
            _context.RolePermissions.Add(rolePermission);
        }
        else
        {
            rolePermission.Permissions = request.Permissions;
            rolePermission.LastModifiedDate = DateTime.UtcNow;
            rolePermission.ModifiedByUserId = currentUserId;
            rolePermission.Notes = request.Notes;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
