using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Queries.GetMyPermissions;

public record GetMyPermissionsQuery : IRequest<Result<UserPermissionsDto?>>;

public class GetMyPermissionsQueryHandler : IRequestHandler<GetMyPermissionsQuery, Result<UserPermissionsDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IUser _user;

    public GetMyPermissionsQueryHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager,
        IUser user)
    {
        _context = context;
        _userManager = userManager;
        _user = user;
    }

    public async Task<Result<UserPermissionsDto?>> Handle(GetMyPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<UserPermissionsDto?>.Failure(new[] { "Unauthorized" });
        }

        var userId = Guid.Parse(_user.Id);
        
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<UserPermissionsDto?>.Failure(new[] { "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        
        var userPermission = await _context.UserPermissions
            .Where(up => up.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        var permissions = userPermission?.Permissions ?? Permission.None;
        
        // Get granted by user
        string? grantedBy = null;
        if (userPermission?.GrantedByUserId != null)
        {
            var grantedByUser = await _userManager.FindByIdAsync(userPermission.GrantedByUserId.Value.ToString());
            grantedBy = grantedByUser != null ? $"{grantedByUser.FirstName} {grantedByUser.LastName}" : null;
        }

        var dto = new UserPermissionsDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles.ToList(),
            Permissions = permissions,
            PermissionList = GetPermissionList(permissions),
            GrantedDate = userPermission?.GrantedDate,
            GrantedBy = grantedBy
        };

        return Result<UserPermissionsDto?>.Success(dto);
    }

    private List<PermissionDto> GetPermissionList(Permission userPermissions)
    {
        var permissionList = new List<PermissionDto>();
        
        foreach (Permission permission in Enum.GetValues(typeof(Permission)))
        {
            if (permission == Permission.None || permission == Permission.AllPermissions)
                continue;
                
            // Skip composite permissions
            if (permission == Permission.FullStudentAccess || 
                permission == Permission.FullAcademyAccess ||
                permission == Permission.FullQAAccess ||
                permission == Permission.FullMuslimTubeAccess ||
                permission == Permission.FullEventAccess)
                continue;

            var isGranted = (userPermissions & permission) == permission;
            
            // Only include granted permissions
            if (isGranted)
            {
                permissionList.Add(new PermissionDto
                {
                    Name = permission.ToString(),
                    DisplayName = GetDisplayName(permission),
                    Category = GetCategory(permission),
                    Value = (long)permission,
                    IsGranted = isGranted
                });
            }
        }

        return permissionList.OrderBy(p => p.Category).ThenBy(p => p.DisplayName).ToList();
    }

    private string GetDisplayName(Permission permission)
    {
        return permission.ToString()
            .Replace("View", "View ")
            .Replace("Create", "Create ")
            .Replace("Edit", "Edit ")
            .Replace("Delete", "Delete ")
            .Replace("Manage", "Manage ");
    }

    private string GetCategory(Permission permission)
    {
        var permissionName = permission.ToString();
        
        if (permissionName.Contains("Student") && !permissionName.Contains("Academy"))
            return "Students";
        if (permissionName.Contains("Instructor"))
            return "Instructors";
        if (permissionName.Contains("Preacher"))
            return "Preachers";
        if (permissionName.Contains("Admin"))
            return "Admins";
        if (permissionName.Contains("Moderator"))
            return "Moderators";
        if (permissionName.Contains("Course") || permissionName.Contains("Lesson") || permissionName.Contains("Quiz") || permissionName.Contains("Enrollment") || permissionName.Contains("Certificate") || permissionName.Contains("Level"))
            return "Academy";
        if (permissionName.Contains("Question") || permissionName.Contains("QA"))
            return "Q&A";
        if (permissionName.Contains("MuslimTube") || permissionName.Contains("Media"))
            return "MuslimTube";
        if (permissionName.Contains("Event") || permissionName.Contains("Registration"))
            return "Events";
        if (permissionName.Contains("Meeting") || permissionName.Contains("Inquiry"))
            return "Meetings & Inquiries";
        if (permissionName.Contains("Notification"))
            return "Notifications";
        if (permissionName.Contains("Permission") || permissionName.Contains("Role"))
            return "Roles & Permissions";

        return "Other";
    }
}
