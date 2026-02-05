using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Permissions;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Permissions.Queries.GetRolePermissions;

public record GetRolePermissionsQuery(UserRole Role) : IRequest<Result<RolePermissionsDto?>>;

public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, Result<RolePermissionsDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public GetRolePermissionsQueryHandler(
        IApplicationDbContext context,
        IPermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    public async Task<Result<RolePermissionsDto?>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var rolePermission = await _context.RolePermissions
            .Where(rp => rp.Role == request.Role)
            .FirstOrDefaultAsync(cancellationToken);

        Permission permissions;
        DateTime? lastModifiedDate = null;
        Guid? modifiedByUserId = null;
        string? notes = null;

        if (rolePermission != null)
        {
            permissions = rolePermission.Permissions;
            lastModifiedDate = rolePermission.LastModifiedDate;
            modifiedByUserId = rolePermission.ModifiedByUserId;
            notes = rolePermission.Notes;
        }
        else
        {
            // Get default permissions for role
            permissions = await _permissionService.GetDefaultPermissionsForRoleAsync(request.Role.ToString());
        }

        // Get modified by user
        string? modifiedBy = null;
        if (modifiedByUserId != null)
        {
            var user = await _context.Users.FindAsync(new object[] { modifiedByUserId }, cancellationToken);
            modifiedBy = user != null ? $"{user.FirstName} {user.LastName}" : null;
        }

        var dto = new RolePermissionsDto
        {
            Role = request.Role,
            RoleName = request.Role.ToString(),
            Permissions = permissions,
            PermissionList = GetPermissionList(permissions),
            LastModifiedDate = lastModifiedDate,
            ModifiedBy = modifiedBy,
            Notes = notes,
            IsCustomized = rolePermission != null
        };

        return Result<RolePermissionsDto?>.Success(dto);
    }

    private List<PermissionDto> GetPermissionList(Permission rolePermissions)
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

            var isGranted = (rolePermissions & permission) == permission;
            
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
            .Replace("Send", "Send ")
            .Replace("Approve", "Approve ")
            .Replace("Publish", "Publish ")
            .Replace("Answer", "Answer ")
            .Trim();
    }

    private string GetCategory(Permission permission)
    {
        var name = permission.ToString();
        
        if (name.Contains("Dashboard") || name.Contains("Analytics"))
            return "Dashboard";
        if (name.Contains("Admin"))
            return "Admin Management";
        if (name.Contains("Student"))
            return "Student Management";
        if (name.Contains("Level"))
            return "Levels";
        if (name.Contains("Course"))
            return "Courses";
        if (name.Contains("Lesson"))
            return "Lessons";
        if (name.Contains("Quiz") || name.Contains("Question"))
            return "Quizzes";
        if (name.Contains("QA"))
            return "Q&A";
        if (name.Contains("MuslimTube") || name.Contains("Video") || name.Contains("Channel"))
            return "MuslimTube";
        if (name.Contains("Notification"))
            return "Notifications";
        if (name.Contains("Meeting"))
            return "Meeting Requests";
        if (name.Contains("Inquiry"))
            return "Inquiry Requests";
        if (name.Contains("Event"))
            return "Events";
        if (name.Contains("Tag"))
            return "Tags";
        if (name.Contains("Setting") || name.Contains("System") || name.Contains("Backup"))
            return "System Settings";
        if (name.Contains("Report") || name.Contains("Financial") || name.Contains("Audit"))
            return "Reporting";
        if (name.Contains("Role") || name.Contains("Permission"))
            return "Roles & Permissions";
            
        return "Other";
    }
}
