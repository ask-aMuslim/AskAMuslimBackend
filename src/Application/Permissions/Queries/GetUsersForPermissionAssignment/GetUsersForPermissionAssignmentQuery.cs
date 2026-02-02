using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Queries.GetUsersForPermissionAssignment;

public record GetUsersForPermissionAssignmentQuery : IRequest<Result<List<UserForPermissionDto>>>;

public class UserForPermissionDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public bool HasCustomPermissions { get; init; }
    public bool IsActive { get; init; }
}

public class GetUsersForPermissionAssignmentQueryHandler : IRequestHandler<GetUsersForPermissionAssignmentQuery, Result<List<UserForPermissionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;

    public GetUsersForPermissionAssignmentQueryHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<Result<List<UserForPermissionDto>>> Handle(GetUsersForPermissionAssignmentQuery request, CancellationToken cancellationToken)
    {
        // Get all users with admin roles (Administrator, Instructor, Preacher)
        var adminRoleUsers = new List<UserForPermissionDto>();

        // Get all users
        var users = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        // Get user permissions
        var userPermissions = await _context.UserPermissions
            .Select(up => up.UserId)
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            
            // Only include users with admin roles (Administrator, Instructor, Preacher)
            if (roles.Contains(Roles.Administrator) || 
                roles.Contains(Roles.Instructor) || 
                roles.Contains(Roles.Preacher))
            {
                adminRoleUsers.Add(new UserForPermissionDto
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Roles = roles.ToList(),
                    HasCustomPermissions = userPermissions.Contains(user.Id),
                    IsActive = user.IsActive
                });
            }
        }

        return Result<List<UserForPermissionDto>>.Success(adminRoleUsers);
    }
}
