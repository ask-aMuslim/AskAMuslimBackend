using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Queries.GetUsersForPermissionAssignment;

public record GetUsersForPermissionAssignmentQuery : IRequest<Result<PaginatedList<UserForPermissionDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

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

public class GetUsersForPermissionAssignmentQueryHandler : IRequestHandler<GetUsersForPermissionAssignmentQuery, Result<PaginatedList<UserForPermissionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IPermissionService _permissionService;

    public GetUsersForPermissionAssignmentQueryHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager,
        IPermissionService permissionService)
    {
        _context = context;
        _userManager = userManager;
        _permissionService = permissionService;
    }

    public async Task<Result<PaginatedList<UserForPermissionDto>>> Handle(GetUsersForPermissionAssignmentQuery request, CancellationToken cancellationToken)
    {
        // Get all active users
        var usersQuery = _context.Users
            .Where(u => u.IsActive);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            usersQuery = usersQuery.Where(u => 
                u.Email!.ToLower().Contains(searchLower) ||
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower));
        }

        var users = await usersQuery
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);

        // Get all user permissions for comparison
        var allUserPermissions = await _context.UserPermissions
            .Where(up => users.Select(u => u.Id).Contains(up.UserId))
            .ToDictionaryAsync(up => up.UserId, up => up.Permissions, cancellationToken);

        // Filter users with admin roles and build DTOs
        var adminRoleUsers = new List<UserForPermissionDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            
            // Only include users with admin roles (Administrator, Instructor, Preacher)
            if (roles.Contains(Roles.Administrator) || 
                roles.Contains(Roles.Instructor) || 
                roles.Contains(Roles.Preacher))
            {
                // Determine if user has custom permissions by comparing with role defaults
                bool hasCustomPermissions = false;
                if (allUserPermissions.TryGetValue(user.Id, out var userPermissions))
                {
                    var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(user.Role.ToString());
                    hasCustomPermissions = userPermissions != rolePermissions;
                }

                adminRoleUsers.Add(new UserForPermissionDto
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    FullName = $"{user.FirstName} {user.LastName}".Trim(),
                    Roles = roles.ToList(),
                    HasCustomPermissions = hasCustomPermissions,
                    IsActive = user.IsActive
                });
            }
        }

        // Apply pagination
        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? 10;
        var totalCount = adminRoleUsers.Count;
        var paginatedItems = adminRoleUsers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var paginatedList = new PaginatedList<UserForPermissionDto>(
            paginatedItems,
            totalCount,
            pageNumber,
            pageSize);

        return Result<PaginatedList<UserForPermissionDto>>.Success(paginatedList);
    }
}
