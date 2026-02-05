using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Infrastructure.Services;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Permission> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Permission.None;
        }

        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userGuid, cancellationToken);

        if (userPermission != null)
        {
            return userPermission.Permissions;
        }

        // If no custom permissions, get default for role
        var user = await _context.Users.FindAsync(new object[] { userGuid }, cancellationToken);
        if (user != null)
        {
            return await GetDefaultPermissionsForRoleAsync(user.Role.ToString());
        }

        return Permission.None;
    }

    public async Task<bool> HasPermissionAsync(string userId, Permission permission, CancellationToken cancellationToken = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return userPermissions.HasFlag(permission);
    }

    public async Task<bool> HasAnyPermissionAsync(string userId, Permission[] permissions, CancellationToken cancellationToken = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Any(p => userPermissions.HasFlag(p));
    }

    public async Task<bool> HasAllPermissionsAsync(string userId, Permission[] permissions, CancellationToken cancellationToken = default)
    {
        var userPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.All(p => userPermissions.HasFlag(p));
    }

    public async Task SetUserPermissionsAsync(string userId, Permission permissions, string? grantedByUserId = null, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            throw new ArgumentException("Invalid user ID", nameof(userId));
        }

        Guid? grantedByGuid = null;
        if (!string.IsNullOrEmpty(grantedByUserId) && Guid.TryParse(grantedByUserId, out var tempGuid))
        {
            grantedByGuid = tempGuid;
        }

        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userGuid, cancellationToken);

        if (userPermission == null)
        {
            userPermission = new UserPermission
            {
                UserId = userGuid,
                Permissions = permissions,
                GrantedByUserId = grantedByGuid,
                Notes = notes
            };
            _context.UserPermissions.Add(userPermission);
        }
        else
        {
            userPermission.Permissions = permissions;
            userPermission.GrantedDate = DateTime.UtcNow;
            userPermission.GrantedByUserId = grantedByGuid;
            userPermission.Notes = notes;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddPermissionsAsync(string userId, Permission permissions, string? grantedByUserId = null, CancellationToken cancellationToken = default)
    {
        var currentPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        var newPermissions = currentPermissions | permissions;
        await SetUserPermissionsAsync(userId, newPermissions, grantedByUserId, null, cancellationToken);
    }

    public async Task RemovePermissionsAsync(string userId, Permission permissions, CancellationToken cancellationToken = default)
    {
        var currentPermissions = await GetUserPermissionsAsync(userId, cancellationToken);
        var newPermissions = currentPermissions & ~permissions;
        await SetUserPermissionsAsync(userId, newPermissions, null, null, cancellationToken);
    }

    public async Task<Permission> GetDefaultPermissionsForRoleAsync(string role)
    {
        if (!Enum.TryParse<UserRole>(role, out var userRole))
        {
            return Permission.None;
        }

        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == userRole);

        if (rolePermission != null)
        {
            return rolePermission.Permissions;
        }

        // Fallback to default permissions if not configured in database
        return userRole switch
        {
            UserRole.Admin => Permission.ViewDashboard | Permission.ViewAnalytics | Permission.ViewAdmins | 
                       Permission.CreateAdmin | Permission.EditAdmin | Permission.DeleteAdmin | 
                       Permission.ManageAdminRoles | Permission.FullStudentAccess | Permission.FullAcademyAccess |
                       Permission.FullQAAccess | Permission.FullMuslimTubeAccess | Permission.ViewNotifications |
                       Permission.SendBulkNotifications | Permission.CreateNotificationTemplate |
                       Permission.ViewMeetingRequests | Permission.ManageMeetingRequests |
                       Permission.ViewInquiryRequests | Permission.ManageInquiryRequests |
                       Permission.FullEventAccess | Permission.ViewTags | Permission.CreateTag | 
                       Permission.EditTag | Permission.DeleteTag | Permission.ViewSettings | 
                       Permission.ViewReports | Permission.ManageRolesAndPermissions |
                       Permission.ViewFinancialData | Permission.ManageSystemBackup | Permission.ViewAuditLogs,

            UserRole.Instructor => Permission.ViewEnrolledStudents | Permission.ViewStudentProgress |
                            Permission.SendNotifications | Permission.ViewStudentQuestions |
                            Permission.AnswerStudentQuestions | Permission.ManageAdminNotes |
                            Permission.SendDirectEmail | Permission.ViewAllCourses | Permission.CreateCourse |
                            Permission.EditOwnCourses | Permission.ViewLessons | Permission.CreateLesson |
                            Permission.EditLesson | Permission.DeleteLesson,

            UserRole.Preacher => Permission.ViewStudentQuestions | Permission.ManageAdminNotes |
                          Permission.FullQAAccess | Permission.FullMuslimTubeAccess | Permission.FullEventAccess,

            UserRole.Student or UserRole.NonMuslim => Permission.None,

            _ => Permission.None
        };
    }
}
