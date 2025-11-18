namespace AskAMuslimBackend.Domain.Enums;

[Flags]
public enum Permission
{
    None = 0,
    
    // User Management
    ViewAllUsers = 1 << 0,              // 1
    EditUserProfile = 1 << 1,           // 2
    ResetUserPassword = 1 << 2,         // 4
    ManageAccountStatus = 1 << 3,       // 8
    AssignRoles = 1 << 4,               // 16
    
    // Student Management
    ViewAllStudents = 1 << 5,           // 32
    ViewEnrolledStudents = 1 << 6,      // 64
    ViewStudentProgress = 1 << 7,       // 128
    SendNotifications = 1 << 8,         // 256
    ViewStudentQuestions = 1 << 9,      // 512
    AnswerStudentQuestions = 1 << 10,   // 1024
    ManageAdminNotes = 1 << 11,         // 2048
    SendDirectEmail = 1 << 12,          // 4096
    
    // Course Management
    ViewAllCourses = 1 << 13,           // 8192
    CreateCourse = 1 << 14,             // 16384
    EditOwnCourses = 1 << 15,           // 32768
    EditAllCourses = 1 << 16,           // 65536
    DeleteCourse = 1 << 17,             // 131072
    ManageLessons = 1 << 18,            // 262144
    ManageCategories = 1 << 19,         // 524288
    
    // Content Management
    ManageQA = 1 << 20,                 // 1048576
    PublishArticles = 1 << 21,          // 2097152
    ModerateDiscussions = 1 << 22,      // 4194304
    ManageDawahContent = 1 << 23,       // 8388608
    
    // System Management
    ViewReports = 1 << 24,              // 16777216
    ManageSystemSettings = 1 << 25,     // 33554432
    ViewFinancialData = 1 << 26,        // 67108864
    ManageRolesAndPermissions = 1 << 27 // 134217728
}
