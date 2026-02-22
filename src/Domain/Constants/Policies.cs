namespace AskAMuslimBackend.Domain.Constants;

public abstract class Policies
{
    // System Policies
    public const string CanPurge = nameof(CanPurge);
    public const string SuperAdminOnly = nameof(SuperAdminOnly);
    
    // Dashboard
    public const string ViewDashboard = nameof(ViewDashboard);
    public const string ViewAnalytics = nameof(ViewAnalytics);
    
    // Admin Management
    public const string ViewAdmins = nameof(ViewAdmins);
    public const string ManageAdmins = nameof(ManageAdmins);
    public const string ManageAdminRoles = nameof(ManageAdminRoles);
    
    // User Management Policies
    public const string ManageAllUsers = nameof(ManageAllUsers);
    public const string ViewAllUsers = nameof(ViewAllUsers);
    public const string AssignRoles = nameof(AssignRoles);
    
    // Student Management Policies
    public const string ViewAllStudents = nameof(ViewAllStudents);
    public const string ViewEnrolledStudents = nameof(ViewEnrolledStudents);
    public const string ManageStudents = nameof(ManageStudents);
    public const string SendNotifications = nameof(SendNotifications);
    public const string ManageAdminNotes = nameof(ManageAdminNotes);
    
    // Academy - Levels
    public const string ViewLevels = nameof(ViewLevels);
    public const string ManageLevels = nameof(ManageLevels);
    
    // Academy - Courses
    public const string ViewAllCourses = nameof(ViewAllCourses);
    public const string ViewOwnCourses = nameof(ViewOwnCourses);
    public const string ManageAllCourses = nameof(ManageAllCourses);
    public const string ManageOwnCourses = nameof(ManageOwnCourses);
    public const string CreateCourse = nameof(CreateCourse);
    public const string PublishCourse = nameof(PublishCourse);
    
    // Academy - Lessons & Quizzes
    public const string ViewLessons = nameof(ViewLessons);
    public const string ManageLessons = nameof(ManageLessons);
    public const string ViewQuizzes = nameof(ViewQuizzes);
    public const string ManageQuizzes = nameof(ManageQuizzes);
    
    // QA Management
    public const string ViewQA = nameof(ViewQA);
    public const string ManageQA = nameof(ManageQA);
    public const string ApproveQA = nameof(ApproveQA);
    
    // MuslimTube
    public const string ViewMuslimTube = nameof(ViewMuslimTube);
    public const string ManageMuslimTube = nameof(ManageMuslimTube);
    public const string ApproveVideos = nameof(ApproveVideos);
    
    // Notifications
    public const string ViewNotifications = nameof(ViewNotifications);
    public const string SendBulkNotifications = nameof(SendBulkNotifications);
    
    // Meeting & Inquiry Requests
    public const string ViewMeetingRequests = nameof(ViewMeetingRequests);
    public const string ManageMeetingRequests = nameof(ManageMeetingRequests);
    public const string ViewInquiryRequests = nameof(ViewInquiryRequests);
    public const string ManageInquiryRequests = nameof(ManageInquiryRequests);
    
    // Events
    public const string ViewEvents = nameof(ViewEvents);
    public const string ManageEvents = nameof(ManageEvents);
    public const string ManageEventRegistrations = nameof(ManageEventRegistrations);
    
    // Tags
    public const string ViewTags = nameof(ViewTags);
    public const string ManageTags = nameof(ManageTags);
    
    // Settings & System
    public const string ViewSettings = nameof(ViewSettings);
    public const string ManageSettings = nameof(ManageSettings);
    public const string ViewReports = nameof(ViewReports);
    public const string ManageReports = nameof(ManageReports);
    public const string ManageRolesAndPermissions = nameof(ManageRolesAndPermissions);
    public const string ViewOwnPermissions = nameof(ViewOwnPermissions);
    public const string ViewFinancialData = nameof(ViewFinancialData);
    public const string ViewAuditLogs = nameof(ViewAuditLogs);
    
    // Legacy compatibility
    public const string ManageContent = nameof(ManageContent);
    public const string PublishArticles = nameof(PublishArticles);
    public const string ManageDawahContent = nameof(ManageDawahContent);
    public const string ManageSystem = nameof(ManageSystem);
}