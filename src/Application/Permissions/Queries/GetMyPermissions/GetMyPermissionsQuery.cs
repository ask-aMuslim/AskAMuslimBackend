using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AskAMuslimBackend.Application.Permissions.Queries.GetMyPermissions;

public record GetMyPermissionsQuery : IRequest<Result<List<PermissionCategoryDto>>>;

public class GetMyPermissionsQueryHandler : IRequestHandler<GetMyPermissionsQuery, Result<List<PermissionCategoryDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IUser _user;
    private readonly IPermissionService _permissionService;

    public GetMyPermissionsQueryHandler(
        IApplicationDbContext context,
        UserManager<Domain.Entities.User> userManager,
        IUser user,
        IPermissionService permissionService)
    {
        _context = context;
        _userManager = userManager;
        _user = user;
        _permissionService = permissionService;
    }

    public async Task<Result<List<PermissionCategoryDto>>> Handle(GetMyPermissionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<List<PermissionCategoryDto>>.Failure(new[] { "Unauthorized" });
        }

        var userId = Guid.Parse(_user.Id);

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result<List<PermissionCategoryDto>>.Failure(new[] { "User not found" });
        }

        // Use PermissionService which properly handles SuperAdmin check
        // SuperAdmins will automatically receive AllPermissions regardless of UserPermissions table
        var userPermissions = await _permissionService.GetUserPermissionsAsync(_user.Id!, cancellationToken);

        var permissions = new List<PermissionCategoryDto>
        {
            new PermissionCategoryDto
            {
                Category = "Dashboard",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewDashboard), DisplayName = "View Dashboard", Category = "Dashboard", Value = (long)Permission.ViewDashboard, IsGranted = HasPermission(userPermissions, Permission.ViewDashboard) },
                    new() { Name = nameof(Permission.ViewAnalytics), DisplayName = "View Analytics", Category = "Dashboard", Value = (long)Permission.ViewAnalytics, IsGranted = HasPermission(userPermissions, Permission.ViewAnalytics) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Admin Management",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewAdmins), DisplayName = "View Admins", Category = "Admin Management", Value = (long)Permission.ViewAdmins, IsGranted = HasPermission(userPermissions, Permission.ViewAdmins) },
                    new() { Name = nameof(Permission.CreateAdmin), DisplayName = "Create Admin", Category = "Admin Management", Value = (long)Permission.CreateAdmin, IsGranted = HasPermission(userPermissions, Permission.CreateAdmin) },
                    new() { Name = nameof(Permission.EditAdmin), DisplayName = "Edit Admin", Category = "Admin Management", Value = (long)Permission.EditAdmin, IsGranted = HasPermission(userPermissions, Permission.EditAdmin) },
                    new() { Name = nameof(Permission.DeleteAdmin), DisplayName = "Delete Admin", Category = "Admin Management", Value = (long)Permission.DeleteAdmin, IsGranted = HasPermission(userPermissions, Permission.DeleteAdmin) },
                    new() { Name = nameof(Permission.ManageAdminRoles), DisplayName = "Manage Admin Roles", Category = "Admin Management", Value = (long)Permission.ManageAdminRoles, IsGranted = HasPermission(userPermissions, Permission.ManageAdminRoles) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Student Management",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewAllStudents), DisplayName = "View All Students", Category = "Student Management", Value = (long)Permission.ViewAllStudents, IsGranted = HasPermission(userPermissions, Permission.ViewAllStudents) },
                    new() { Name = nameof(Permission.ViewEnrolledStudents), DisplayName = "View Enrolled Students", Category = "Student Management", Value = (long)Permission.ViewEnrolledStudents, IsGranted = HasPermission(userPermissions, Permission.ViewEnrolledStudents) },
                    new() { Name = nameof(Permission.EditStudent), DisplayName = "Edit Student", Category = "Student Management", Value = (long)Permission.EditStudent, IsGranted = HasPermission(userPermissions, Permission.EditStudent) },
                    new() { Name = nameof(Permission.DeleteStudent), DisplayName = "Delete Student", Category = "Student Management", Value = (long)Permission.DeleteStudent, IsGranted = HasPermission(userPermissions, Permission.DeleteStudent) },
                    new() { Name = nameof(Permission.ViewStudentProgress), DisplayName = "View Student Progress", Category = "Student Management", Value = (long)Permission.ViewStudentProgress, IsGranted = HasPermission(userPermissions, Permission.ViewStudentProgress) },
                    new() { Name = nameof(Permission.ViewStudentQuestions), DisplayName = "View Student Questions", Category = "Student Management", Value = (long)Permission.ViewStudentQuestions, IsGranted = HasPermission(userPermissions, Permission.ViewStudentQuestions) },
                    new() { Name = nameof(Permission.AnswerStudentQuestions), DisplayName = "Answer Student Questions", Category = "Student Management", Value = (long)Permission.AnswerStudentQuestions, IsGranted = HasPermission(userPermissions, Permission.AnswerStudentQuestions) },
                    new() { Name = nameof(Permission.ManageAdminNotes), DisplayName = "Manage Admin Notes", Category = "Student Management", Value = (long)Permission.ManageAdminNotes, IsGranted = HasPermission(userPermissions, Permission.ManageAdminNotes) },
                    new() { Name = nameof(Permission.SendNotifications), DisplayName = "Send Notifications", Category = "Student Management", Value = (long)Permission.SendNotifications, IsGranted = HasPermission(userPermissions, Permission.SendNotifications) },
                    new() { Name = nameof(Permission.SendDirectEmail), DisplayName = "Send Direct Email", Category = "Student Management", Value = (long)Permission.SendDirectEmail, IsGranted = HasPermission(userPermissions, Permission.SendDirectEmail) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Academy - Levels",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewLevels), DisplayName = "View Levels", Category = "Academy - Levels", Value = (long)Permission.ViewLevels, IsGranted = HasPermission(userPermissions, Permission.ViewLevels) },
                    new() { Name = nameof(Permission.CreateLevel), DisplayName = "Create Level", Category = "Academy - Levels", Value = (long)Permission.CreateLevel, IsGranted = HasPermission(userPermissions, Permission.CreateLevel) },
                    new() { Name = nameof(Permission.EditLevel), DisplayName = "Edit Level", Category = "Academy - Levels", Value = (long)Permission.EditLevel, IsGranted = HasPermission(userPermissions, Permission.EditLevel) },
                    new() { Name = nameof(Permission.DeleteLevel), DisplayName = "Delete Level", Category = "Academy - Levels", Value = (long)Permission.DeleteLevel, IsGranted = HasPermission(userPermissions, Permission.DeleteLevel) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Academy - Courses",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewAllCourses), DisplayName = "View All Courses", Category = "Academy - Courses", Value = (long)Permission.ViewAllCourses, IsGranted = HasPermission(userPermissions, Permission.ViewAllCourses) },
                    new() { Name = nameof(Permission.ViewOwnCourses), DisplayName = "View Own Courses", Category = "Academy - Courses", Value = (long)Permission.ViewOwnCourses, IsGranted = HasPermission(userPermissions, Permission.ViewOwnCourses) },
                    new() { Name = nameof(Permission.CreateCourse), DisplayName = "Create Course", Category = "Academy - Courses", Value = (long)Permission.CreateCourse, IsGranted = HasPermission(userPermissions, Permission.CreateCourse) },
                    new() { Name = nameof(Permission.EditOwnCourses), DisplayName = "Edit Own Courses", Category = "Academy - Courses", Value = (long)Permission.EditOwnCourses, IsGranted = HasPermission(userPermissions, Permission.EditOwnCourses) },
                    new() { Name = nameof(Permission.EditAllCourses), DisplayName = "Edit All Courses", Category = "Academy - Courses", Value = (long)Permission.EditAllCourses, IsGranted = HasPermission(userPermissions, Permission.EditAllCourses) },
                    new() { Name = nameof(Permission.DeleteCourse), DisplayName = "Delete Course", Category = "Academy - Courses", Value = (long)Permission.DeleteCourse, IsGranted = HasPermission(userPermissions, Permission.DeleteCourse) },
                    new() { Name = nameof(Permission.PublishCourse), DisplayName = "Publish Course", Category = "Academy - Courses", Value = (long)Permission.PublishCourse, IsGranted = HasPermission(userPermissions, Permission.PublishCourse) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Academy - Lessons",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewLessons), DisplayName = "View Lessons", Category = "Academy - Lessons", Value = (long)Permission.ViewLessons, IsGranted = HasPermission(userPermissions, Permission.ViewLessons) },
                    new() { Name = nameof(Permission.CreateLesson), DisplayName = "Create Lesson", Category = "Academy - Lessons", Value = (long)Permission.CreateLesson, IsGranted = HasPermission(userPermissions, Permission.CreateLesson) },
                    new() { Name = nameof(Permission.EditLesson), DisplayName = "Edit Lesson", Category = "Academy - Lessons", Value = (long)Permission.EditLesson, IsGranted = HasPermission(userPermissions, Permission.EditLesson) },
                    new() { Name = nameof(Permission.DeleteLesson), DisplayName = "Delete Lesson", Category = "Academy - Lessons", Value = (long)Permission.DeleteLesson, IsGranted = HasPermission(userPermissions, Permission.DeleteLesson) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Academy - Quizzes",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewQuizzes), DisplayName = "View Quizzes", Category = "Academy - Quizzes", Value = (long)Permission.ViewQuizzes, IsGranted = HasPermission(userPermissions, Permission.ViewQuizzes) },
                    new() { Name = nameof(Permission.CreateQuiz), DisplayName = "Create Quiz", Category = "Academy - Quizzes", Value = (long)Permission.CreateQuiz, IsGranted = HasPermission(userPermissions, Permission.CreateQuiz) },
                    new() { Name = nameof(Permission.EditQuiz), DisplayName = "Edit Quiz", Category = "Academy - Quizzes", Value = (long)Permission.EditQuiz, IsGranted = HasPermission(userPermissions, Permission.EditQuiz) },
                    new() { Name = nameof(Permission.DeleteQuiz), DisplayName = "Delete Quiz", Category = "Academy - Quizzes", Value = (long)Permission.DeleteQuiz, IsGranted = HasPermission(userPermissions, Permission.DeleteQuiz) },
                    new() { Name = nameof(Permission.ViewQuizResults), DisplayName = "View Quiz Results", Category = "Academy - Quizzes", Value = (long)Permission.ViewQuizResults, IsGranted = HasPermission(userPermissions, Permission.ViewQuizResults) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "QA Management",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewQA), DisplayName = "View QA", Category = "QA Management", Value = (long)Permission.ViewQA, IsGranted = HasPermission(userPermissions, Permission.ViewQA) },
                    new() { Name = nameof(Permission.CreateQA), DisplayName = "Create QA", Category = "QA Management", Value = (long)Permission.CreateQA, IsGranted = HasPermission(userPermissions, Permission.CreateQA) },
                    new() { Name = nameof(Permission.EditQA), DisplayName = "Edit QA", Category = "QA Management", Value = (long)Permission.EditQA, IsGranted = HasPermission(userPermissions, Permission.EditQA) },
                    new() { Name = nameof(Permission.DeleteQA), DisplayName = "Delete QA", Category = "QA Management", Value = (long)Permission.DeleteQA, IsGranted = HasPermission(userPermissions, Permission.DeleteQA) },
                    new() { Name = nameof(Permission.ApproveQA), DisplayName = "Approve QA", Category = "QA Management", Value = (long)Permission.ApproveQA, IsGranted = HasPermission(userPermissions, Permission.ApproveQA) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "MuslimTube",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewMuslimTube), DisplayName = "View MuslimTube", Category = "MuslimTube", Value = (long)Permission.ViewMuslimTube, IsGranted = HasPermission(userPermissions, Permission.ViewMuslimTube) },
                    new() { Name = nameof(Permission.CreateVideo), DisplayName = "Create Video", Category = "MuslimTube", Value = (long)Permission.CreateVideo, IsGranted = HasPermission(userPermissions, Permission.CreateVideo) },
                    new() { Name = nameof(Permission.EditVideo), DisplayName = "Edit Video", Category = "MuslimTube", Value = (long)Permission.EditVideo, IsGranted = HasPermission(userPermissions, Permission.EditVideo) },
                    new() { Name = nameof(Permission.DeleteVideo), DisplayName = "Delete Video", Category = "MuslimTube", Value = (long)Permission.DeleteVideo, IsGranted = HasPermission(userPermissions, Permission.DeleteVideo) },
                    new() { Name = nameof(Permission.ApproveVideo), DisplayName = "Approve Video", Category = "MuslimTube", Value = (long)Permission.ApproveVideo, IsGranted = HasPermission(userPermissions, Permission.ApproveVideo) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Notifications",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewNotifications), DisplayName = "View Notifications", Category = "Notifications", Value = (long)Permission.ViewNotifications, IsGranted = HasPermission(userPermissions, Permission.ViewNotifications) },
                    new() { Name = nameof(Permission.SendBulkNotifications), DisplayName = "Send Bulk Notifications", Category = "Notifications", Value = (long)Permission.SendBulkNotifications, IsGranted = HasPermission(userPermissions, Permission.SendBulkNotifications) },
                    new() { Name = nameof(Permission.CreateNotificationTemplate), DisplayName = "Create Notification Template", Category = "Notifications", Value = (long)Permission.CreateNotificationTemplate, IsGranted = HasPermission(userPermissions, Permission.CreateNotificationTemplate) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Meeting & Inquiry Requests",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewMeetingRequests), DisplayName = "View Meeting Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ViewMeetingRequests, IsGranted = HasPermission(userPermissions, Permission.ViewMeetingRequests) },
                    new() { Name = nameof(Permission.ManageMeetingRequests), DisplayName = "Manage Meeting Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ManageMeetingRequests, IsGranted = HasPermission(userPermissions, Permission.ManageMeetingRequests) },
                    new() { Name = nameof(Permission.ViewInquiryRequests), DisplayName = "View Inquiry Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ViewInquiryRequests, IsGranted = HasPermission(userPermissions, Permission.ViewInquiryRequests) },
                    new() { Name = nameof(Permission.ManageInquiryRequests), DisplayName = "Manage Inquiry Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ManageInquiryRequests, IsGranted = HasPermission(userPermissions, Permission.ManageInquiryRequests) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Events",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewEvents), DisplayName = "View Events", Category = "Events", Value = (long)Permission.ViewEvents, IsGranted = HasPermission(userPermissions, Permission.ViewEvents) },
                    new() { Name = nameof(Permission.CreateEvent), DisplayName = "Create Event", Category = "Events", Value = (long)Permission.CreateEvent, IsGranted = HasPermission(userPermissions, Permission.CreateEvent) },
                    new() { Name = nameof(Permission.EditEvent), DisplayName = "Edit Event", Category = "Events", Value = (long)Permission.EditEvent, IsGranted = HasPermission(userPermissions, Permission.EditEvent) },
                    new() { Name = nameof(Permission.DeleteEvent), DisplayName = "Delete Event", Category = "Events", Value = (long)Permission.DeleteEvent, IsGranted = HasPermission(userPermissions, Permission.DeleteEvent) },
                    new() { Name = nameof(Permission.ManageEventRegistrations), DisplayName = "Manage Event Registrations", Category = "Events", Value = (long)Permission.ManageEventRegistrations, IsGranted = HasPermission(userPermissions, Permission.ManageEventRegistrations) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Tags",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewTags), DisplayName = "View Tags", Category = "Tags", Value = (long)Permission.ViewTags, IsGranted = HasPermission(userPermissions, Permission.ViewTags) },
                    new() { Name = nameof(Permission.CreateTag), DisplayName = "Create Tag", Category = "Tags", Value = (long)Permission.CreateTag, IsGranted = HasPermission(userPermissions, Permission.CreateTag) },
                    new() { Name = nameof(Permission.EditTag), DisplayName = "Edit Tag", Category = "Tags", Value = (long)Permission.EditTag, IsGranted = HasPermission(userPermissions, Permission.EditTag) },
                    new() { Name = nameof(Permission.DeleteTag), DisplayName = "Delete Tag", Category = "Tags", Value = (long)Permission.DeleteTag, IsGranted = HasPermission(userPermissions, Permission.DeleteTag) }
                }
            },
            new PermissionCategoryDto
            {
                Category = "Settings & System",
                Permissions = new List<PermissionDto>
                {
                    new() { Name = nameof(Permission.ViewSettings), DisplayName = "View Settings", Category = "Settings & System", Value = (long)Permission.ViewSettings, IsGranted = HasPermission(userPermissions, Permission.ViewSettings) },
                    new() { Name = nameof(Permission.ManageSettings), DisplayName = "Manage Settings", Category = "Settings & System", Value = (long)Permission.ManageSettings, IsGranted = HasPermission(userPermissions, Permission.ManageSettings) },
                    new() { Name = nameof(Permission.ViewReports), DisplayName = "View Reports", Category = "Settings & System", Value = (long)Permission.ViewReports, IsGranted = HasPermission(userPermissions, Permission.ViewReports) },
                    new() { Name = nameof(Permission.ManageRolesAndPermissions), DisplayName = "Manage Roles & Permissions", Category = "Settings & System", Value = (long)Permission.ManageRolesAndPermissions, IsGranted = HasPermission(userPermissions, Permission.ManageRolesAndPermissions) },
                    new() { Name = nameof(Permission.ViewFinancialData), DisplayName = "View Financial Data", Category = "Settings & System", Value = (long)Permission.ViewFinancialData, IsGranted = HasPermission(userPermissions, Permission.ViewFinancialData) },
                    new() { Name = nameof(Permission.ManageSystemBackup), DisplayName = "Manage System Backup", Category = "Settings & System", Value = (long)Permission.ManageSystemBackup, IsGranted = HasPermission(userPermissions, Permission.ManageSystemBackup) },
                    new() { Name = nameof(Permission.ViewAuditLogs), DisplayName = "View Audit Logs", Category = "Settings & System", Value = (long)Permission.ViewAuditLogs, IsGranted = HasPermission(userPermissions, Permission.ViewAuditLogs) }
                }
            }
        };

        return Result<List<PermissionCategoryDto>>.Success(permissions);
    }

    private bool HasPermission(Permission userPermissions, Permission permission)
    {
        return (userPermissions & permission) == permission;
    }
}
