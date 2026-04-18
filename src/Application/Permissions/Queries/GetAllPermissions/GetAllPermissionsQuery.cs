using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Permissions.Queries.GetAllPermissions;

public record GetAllPermissionsQuery : IRequest<Result<List<PermissionCategoryDefinitionDto>>>;

public class GetAllPermissionsQueryHandler : IRequestHandler<GetAllPermissionsQuery, Result<List<PermissionCategoryDefinitionDto>>>
{
    public Task<Result<List<PermissionCategoryDefinitionDto>>> Handle(GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = new List<PermissionCategoryDefinitionDto>
        {
            new PermissionCategoryDefinitionDto
            {
                Category = "Dashboard",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewDashboard), DisplayName = "View Dashboard", Category = "Dashboard", Value = (long)Permission.ViewDashboard },
                    new() { Name = nameof(Permission.ViewAnalytics), DisplayName = "View Analytics", Category = "Dashboard", Value = (long)Permission.ViewAnalytics }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Admin Management",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewAdmins), DisplayName = "View Admins", Category = "Admin Management", Value = (long)Permission.ViewAdmins },
                    new() { Name = nameof(Permission.CreateAdmin), DisplayName = "Create Admin", Category = "Admin Management", Value = (long)Permission.CreateAdmin },
                    new() { Name = nameof(Permission.EditAdmin), DisplayName = "Edit Admin", Category = "Admin Management", Value = (long)Permission.EditAdmin },
                    new() { Name = nameof(Permission.DeleteAdmin), DisplayName = "Delete Admin", Category = "Admin Management", Value = (long)Permission.DeleteAdmin },
                    new() { Name = nameof(Permission.ManageAdminRoles), DisplayName = "Manage Admin Roles", Category = "Admin Management", Value = (long)Permission.ManageAdminRoles }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Student Management",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewAllStudents), DisplayName = "View All Students", Category = "Student Management", Value = (long)Permission.ViewAllStudents },
                    new() { Name = nameof(Permission.ViewEnrolledStudents), DisplayName = "View Enrolled Students", Category = "Student Management", Value = (long)Permission.ViewEnrolledStudents },
                    new() { Name = nameof(Permission.EditStudent), DisplayName = "Edit Student", Category = "Student Management", Value = (long)Permission.EditStudent },
                    new() { Name = nameof(Permission.DeleteStudent), DisplayName = "Delete Student", Category = "Student Management", Value = (long)Permission.DeleteStudent },
                    new() { Name = nameof(Permission.ViewStudentProgress), DisplayName = "View Student Progress", Category = "Student Management", Value = (long)Permission.ViewStudentProgress },
                    new() { Name = nameof(Permission.ViewStudentQuestions), DisplayName = "View Student Questions", Category = "Student Management", Value = (long)Permission.ViewStudentQuestions },
                    new() { Name = nameof(Permission.AnswerStudentQuestions), DisplayName = "Answer Student Questions", Category = "Student Management", Value = (long)Permission.AnswerStudentQuestions },
                    new() { Name = nameof(Permission.ManageAdminNotes), DisplayName = "Manage Admin Notes", Category = "Student Management", Value = (long)Permission.ManageAdminNotes },
                    new() { Name = nameof(Permission.SendNotifications), DisplayName = "Send Notifications", Category = "Student Management", Value = (long)Permission.SendNotifications },
                    new() { Name = nameof(Permission.SendDirectEmail), DisplayName = "Send Direct Email", Category = "Student Management", Value = (long)Permission.SendDirectEmail }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Academy - Levels",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewLevels), DisplayName = "View Levels", Category = "Academy - Levels", Value = (long)Permission.ViewLevels },
                    new() { Name = nameof(Permission.CreateLevel), DisplayName = "Create Level", Category = "Academy - Levels", Value = (long)Permission.CreateLevel },
                    new() { Name = nameof(Permission.EditLevel), DisplayName = "Edit Level", Category = "Academy - Levels", Value = (long)Permission.EditLevel },
                    new() { Name = nameof(Permission.DeleteLevel), DisplayName = "Delete Level", Category = "Academy - Levels", Value = (long)Permission.DeleteLevel }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Academy - Courses",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewAllCourses), DisplayName = "View All Courses", Category = "Academy - Courses", Value = (long)Permission.ViewAllCourses },
                    new() { Name = nameof(Permission.ViewOwnCourses), DisplayName = "View Own Courses", Category = "Academy - Courses", Value = (long)Permission.ViewOwnCourses },
                    new() { Name = nameof(Permission.CreateCourse), DisplayName = "Create Course", Category = "Academy - Courses", Value = (long)Permission.CreateCourse },
                    new() { Name = nameof(Permission.EditOwnCourses), DisplayName = "Edit Own Courses", Category = "Academy - Courses", Value = (long)Permission.EditOwnCourses },
                    new() { Name = nameof(Permission.EditAllCourses), DisplayName = "Edit All Courses", Category = "Academy - Courses", Value = (long)Permission.EditAllCourses },
                    new() { Name = nameof(Permission.DeleteCourse), DisplayName = "Delete Course", Category = "Academy - Courses", Value = (long)Permission.DeleteCourse },
                    new() { Name = nameof(Permission.PublishCourse), DisplayName = "Publish Course", Category = "Academy - Courses", Value = (long)Permission.PublishCourse }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Academy - Lessons",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewLessons), DisplayName = "View Lessons", Category = "Academy - Lessons", Value = (long)Permission.ViewLessons },
                    new() { Name = nameof(Permission.CreateLesson), DisplayName = "Create Lesson", Category = "Academy - Lessons", Value = (long)Permission.CreateLesson },
                    new() { Name = nameof(Permission.EditLesson), DisplayName = "Edit Lesson", Category = "Academy - Lessons", Value = (long)Permission.EditLesson },
                    new() { Name = nameof(Permission.DeleteLesson), DisplayName = "Delete Lesson", Category = "Academy - Lessons", Value = (long)Permission.DeleteLesson }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Academy - Quizzes",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewQuizzes), DisplayName = "View Quizzes", Category = "Academy - Quizzes", Value = (long)Permission.ViewQuizzes },
                    new() { Name = nameof(Permission.CreateQuiz), DisplayName = "Create Quiz", Category = "Academy - Quizzes", Value = (long)Permission.CreateQuiz },
                    new() { Name = nameof(Permission.EditQuiz), DisplayName = "Edit Quiz", Category = "Academy - Quizzes", Value = (long)Permission.EditQuiz },
                    new() { Name = nameof(Permission.DeleteQuiz), DisplayName = "Delete Quiz", Category = "Academy - Quizzes", Value = (long)Permission.DeleteQuiz },
                    new() { Name = nameof(Permission.ViewQuizResults), DisplayName = "View Quiz Results", Category = "Academy - Quizzes", Value = (long)Permission.ViewQuizResults }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "QA Management",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewQA), DisplayName = "View QA", Category = "QA Management", Value = (long)Permission.ViewQA },
                    new() { Name = nameof(Permission.CreateQA), DisplayName = "Create QA", Category = "QA Management", Value = (long)Permission.CreateQA },
                    new() { Name = nameof(Permission.EditQA), DisplayName = "Edit QA", Category = "QA Management", Value = (long)Permission.EditQA },
                    new() { Name = nameof(Permission.DeleteQA), DisplayName = "Delete QA", Category = "QA Management", Value = (long)Permission.DeleteQA },
                    new() { Name = nameof(Permission.ApproveQA), DisplayName = "Approve QA", Category = "QA Management", Value = (long)Permission.ApproveQA }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "MuslimTube",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewMuslimTube), DisplayName = "View MuslimTube", Category = "MuslimTube", Value = (long)Permission.ViewMuslimTube },
                    new() { Name = nameof(Permission.CreateVideo), DisplayName = "Create Video", Category = "MuslimTube", Value = (long)Permission.CreateVideo },
                    new() { Name = nameof(Permission.EditVideo), DisplayName = "Edit Video", Category = "MuslimTube", Value = (long)Permission.EditVideo },
                    new() { Name = nameof(Permission.DeleteVideo), DisplayName = "Delete Video", Category = "MuslimTube", Value = (long)Permission.DeleteVideo },
                    new() { Name = nameof(Permission.ApproveVideo), DisplayName = "Approve Video", Category = "MuslimTube", Value = (long)Permission.ApproveVideo }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Notifications",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewNotifications), DisplayName = "View Notifications", Category = "Notifications", Value = (long)Permission.ViewNotifications },
                    new() { Name = nameof(Permission.SendBulkNotifications), DisplayName = "Send Bulk Notifications", Category = "Notifications", Value = (long)Permission.SendBulkNotifications },
                    new() { Name = nameof(Permission.CreateNotificationTemplate), DisplayName = "Create Notification Template", Category = "Notifications", Value = (long)Permission.CreateNotificationTemplate }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Meeting & Inquiry Requests",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewMeetingRequests), DisplayName = "View Meeting Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ViewMeetingRequests },
                    new() { Name = nameof(Permission.ManageMeetingRequests), DisplayName = "Manage Meeting Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ManageMeetingRequests },
                    new() { Name = nameof(Permission.ViewInquiryRequests), DisplayName = "View Inquiry Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ViewInquiryRequests },
                    new() { Name = nameof(Permission.ManageInquiryRequests), DisplayName = "Manage Inquiry Requests", Category = "Meeting & Inquiry Requests", Value = (long)Permission.ManageInquiryRequests }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Events",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewEvents), DisplayName = "View Events", Category = "Events", Value = (long)Permission.ViewEvents },
                    new() { Name = nameof(Permission.CreateEvent), DisplayName = "Create Event", Category = "Events", Value = (long)Permission.CreateEvent },
                    new() { Name = nameof(Permission.EditEvent), DisplayName = "Edit Event", Category = "Events", Value = (long)Permission.EditEvent },
                    new() { Name = nameof(Permission.DeleteEvent), DisplayName = "Delete Event", Category = "Events", Value = (long)Permission.DeleteEvent },
                    new() { Name = nameof(Permission.ManageEventRegistrations), DisplayName = "Manage Event Registrations", Category = "Events", Value = (long)Permission.ManageEventRegistrations }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Tags",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewTags), DisplayName = "View Tags", Category = "Tags", Value = (long)Permission.ViewTags },
                    new() { Name = nameof(Permission.CreateTag), DisplayName = "Create Tag", Category = "Tags", Value = (long)Permission.CreateTag },
                    new() { Name = nameof(Permission.EditTag), DisplayName = "Edit Tag", Category = "Tags", Value = (long)Permission.EditTag },
                    new() { Name = nameof(Permission.DeleteTag), DisplayName = "Delete Tag", Category = "Tags", Value = (long)Permission.DeleteTag }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Forms",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewForms), DisplayName = "View Forms", Category = "Forms", Value = (long)Permission.ViewForms },
                    new() { Name = nameof(Permission.ManageForms), DisplayName = "Manage Forms", Category = "Forms", Value = (long)Permission.ManageForms }
                }
            },
            new PermissionCategoryDefinitionDto
            {
                Category = "Settings & System",
                Permissions = new List<PermissionDefinitionDto>
                {
                    new() { Name = nameof(Permission.ViewSettings), DisplayName = "View Settings", Category = "Settings & System", Value = (long)Permission.ViewSettings },
                    new() { Name = nameof(Permission.ManageSettings), DisplayName = "Manage Settings", Category = "Settings & System", Value = (long)Permission.ManageSettings },
                    new() { Name = nameof(Permission.ViewReports), DisplayName = "View Reports", Category = "Settings & System", Value = (long)Permission.ViewReports },
                    new() { Name = nameof(Permission.ManageRolesAndPermissions), DisplayName = "Manage Roles & Permissions", Category = "Settings & System", Value = (long)Permission.ManageRolesAndPermissions },
                    new() { Name = nameof(Permission.ViewFinancialData), DisplayName = "View Financial Data", Category = "Settings & System", Value = (long)Permission.ViewFinancialData },
                    new() { Name = nameof(Permission.ManageSystemBackup), DisplayName = "Manage System Backup", Category = "Settings & System", Value = (long)Permission.ManageSystemBackup },
                    new() { Name = nameof(Permission.ViewAuditLogs), DisplayName = "View Audit Logs", Category = "Settings & System", Value = (long)Permission.ViewAuditLogs }
                }
            }
        };

        return Task.FromResult(Result<List<PermissionCategoryDefinitionDto>>.Success(permissions));
    }
}
