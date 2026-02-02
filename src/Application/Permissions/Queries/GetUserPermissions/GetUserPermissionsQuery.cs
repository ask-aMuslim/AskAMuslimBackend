using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Queries.GetUserPermissions;

public record GetUserPermissionsQuery(Guid UserId) : IRequest<Result<UserPermissionsDto?>>;

public class GetUserPermissionsQueryHandler : IRequestHandler<GetUserPermissionsQuery, Result<UserPermissionsDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;

    public GetUserPermissionsQueryHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<Result<UserPermissionsDto?>> Handle(GetUserPermissionsQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            return Result<UserPermissionsDto?>.Failure(new[] { "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        
        var userPermission = await _context.UserPermissions
            .Where(up => up.UserId == request.UserId)
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
            
            permissionList.Add(new PermissionDto
            {
                Name = permission.ToString(),
                DisplayName = GetDisplayName(permission),
                Category = GetCategory(permission),
                Value = (long)permission,
                IsGranted = isGranted
            });
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
            .Replace("Manage", "Manage ")
            .Replace("Approve", "Approve ")
            .Replace("Send", "Send ")
            .Trim();
    }

    private string GetCategory(Permission permission)
    {
        var name = permission.ToString();
        
        if (name.Contains("Dashboard") || name.Contains("Analytics"))
            return "Dashboard";
        if (name.Contains("Admin") && !name.Contains("Notes"))
            return "Admin Management";
        if (name.Contains("Student"))
            return "Student Management";
        if (name.Contains("Level"))
            return "Academy - Levels";
        if (name.Contains("Course"))
            return "Academy - Courses";
        if (name.Contains("Lesson"))
            return "Academy - Lessons";
        if (name.Contains("Quiz"))
            return "Academy - Quizzes";
        if (name.Contains("QA"))
            return "QA Management";
        if (name.Contains("Video") || name.Contains("MuslimTube"))
            return "MuslimTube";
        if (name.Contains("Notification"))
            return "Notifications";
        if (name.Contains("Meeting") || name.Contains("Inquiry"))
            return "Meeting & Inquiry Requests";
        if (name.Contains("Event"))
            return "Events";
        if (name.Contains("Tag"))
            return "Tags";
        if (name.Contains("Settings") || name.Contains("Reports") || name.Contains("Roles") || 
            name.Contains("Financial") || name.Contains("Backup") || name.Contains("Audit"))
            return "Settings & System";
            
        return "Other";
    }
}
