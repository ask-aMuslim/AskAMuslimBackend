namespace AskAMuslimBackend.Domain.Constants;

public abstract class Policies
{
    // System Policies
    public const string CanPurge = nameof(CanPurge);
    public const string SuperAdminOnly = nameof(SuperAdminOnly);
    
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
    
    // Course Management Policies
    public const string ViewAllCourses = nameof(ViewAllCourses);
    public const string ManageAllCourses = nameof(ManageAllCourses);
    public const string ManageOwnCourses = nameof(ManageOwnCourses);
    public const string CreateCourse = nameof(CreateCourse);
    
    // Content Management Policies
    public const string ManageContent = nameof(ManageContent);
    public const string ManageQA = nameof(ManageQA);
    public const string PublishArticles = nameof(PublishArticles);
    public const string ManageDawahContent = nameof(ManageDawahContent);
    
    // System Policies
    public const string ViewReports = nameof(ViewReports);
    public const string ManageSystem = nameof(ManageSystem);
    public const string ViewFinancialData = nameof(ViewFinancialData);
    public const string ManageRolesAndPermissions = nameof(ManageRolesAndPermissions);
}