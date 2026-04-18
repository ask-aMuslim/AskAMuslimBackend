namespace AskAMuslimBackend.Domain.Enums;

[Flags]
public enum Permission : long
{
    None = 0,

    // Dashboard
    ViewDashboard = 1L << 0,                    // 1
    ViewAnalytics = 1L << 1,                    // 2

    // Admin Management
    ViewAdmins = 1L << 2,                       // 4
    CreateAdmin = 1L << 3,                      // 8
    EditAdmin = 1L << 4,                        // 16
    DeleteAdmin = 1L << 5,                      // 32
    ManageAdminRoles = 1L << 6,                 // 64

    // Student Management
    ViewAllStudents = 1L << 7,                  // 128
    ViewEnrolledStudents = 1L << 8,             // 256
    EditStudent = 1L << 9,                      // 512
    DeleteStudent = 1L << 10,                   // 1024
    ViewStudentProgress = 1L << 11,             // 2048
    ViewStudentQuestions = 1L << 12,            // 4096
    AnswerStudentQuestions = 1L << 13,          // 8192
    ManageAdminNotes = 1L << 14,                // 16384
    SendNotifications = 1L << 15,               // 32768
    SendDirectEmail = 1L << 16,                 // 65536

    // Academy - Levels
    ViewLevels = 1L << 17,                      // 131072
    CreateLevel = 1L << 18,                     // 262144
    EditLevel = 1L << 19,                       // 524288
    DeleteLevel = 1L << 20,                     // 1048576

    // Academy - Courses
    ViewAllCourses = 1L << 21,                  // 2097152
    ViewOwnCourses = 1L << 22,                  // 4194304
    CreateCourse = 1L << 23,                    // 8388608
    EditOwnCourses = 1L << 24,                  // 16777216
    EditAllCourses = 1L << 25,                  // 33554432
    DeleteCourse = 1L << 26,                    // 67108864
    PublishCourse = 1L << 27,                   // 134217728

    // Academy - Lessons
    ViewLessons = 1L << 28,                     // 268435456
    CreateLesson = 1L << 29,                    // 536870912
    EditLesson = 1L << 30,                      // 1073741824
    DeleteLesson = 1L << 31,                    // 2147483648

    // Academy - Quizzes
    ViewQuizzes = 1L << 32,                     // 4294967296
    CreateQuiz = 1L << 33,                      // 8589934592
    EditQuiz = 1L << 34,                        // 17179869184
    DeleteQuiz = 1L << 35,                      // 34359738368
    ViewQuizResults = 1L << 36,                 // 68719476736

    // QA Management
    ViewQA = 1L << 37,                          // 137438953472
    CreateQA = 1L << 38,                        // 274877906944
    EditQA = 1L << 39,                          // 549755813888
    DeleteQA = 1L << 40,                        // 1099511627776
    ApproveQA = 1L << 41,                       // 2199023255552

    // MuslimTube
    ViewMuslimTube = 1L << 42,                  // 4398046511104
    CreateVideo = 1L << 43,                     // 8796093022208
    EditVideo = 1L << 44,                       // 17592186044416
    DeleteVideo = 1L << 45,                     // 35184372088832
    ApproveVideo = 1L << 46,                    // 70368744177664

    // Notifications
    ViewNotifications = 1L << 47,               // 140737488355328
    SendBulkNotifications = 1L << 48,           // 281474976710656
    CreateNotificationTemplate = 1L << 49,      // 562949953421312

    // Meeting & Inquiry Requests
    ViewMeetingRequests = 1L << 50,             // 1125899906842624
    ManageMeetingRequests = 1L << 51,           // 2251799813685248
    ViewInquiryRequests = 1L << 52,             // 4503599627370496
    ManageInquiryRequests = 1L << 53,           // 9007199254740992

    // Events
    ViewEvents = 1L << 54,                      // 18014398509481984
    CreateEvent = 1L << 55,                     // 36028797018963968
    EditEvent = 1L << 56,                       // 72057594037927936
    DeleteEvent = 1L << 57,                     // 144115188075855872
    ManageEventRegistrations = 1L << 58,        // 288230376151711744

    // Tags
    ViewTags = 1L << 59,                        // 576460752303423488
    CreateTag = 1L << 60,                       // 1152921504606846976
    EditTag = 1L << 61,                         // 2305843009213693952
    DeleteTag = 1L << 62,                       // 4611686018427387904

    // Forms
    ViewForms = ViewSettings | ViewDashboard,
    ManageForms = ViewSettings | ViewDashboard | ManageAdminRoles,

    // Settings & System (using last bit)
    ViewSettings = 1L << 63,                    // 9223372036854775808 (max for long, bit 63)
                                                // Note: Cannot use bits beyond 63 in a long. Use composite permissions instead.

    // Composite Permissions (combinations for convenience)
    ManageSettings = ViewSettings,              // Alias for ViewSettings
    ViewReports = ViewDashboard | ViewAnalytics,
    ManageRolesAndPermissions = ManageAdminRoles,
    ViewFinancialData = ViewReports,
    ManageSystemBackup = ViewSettings,
    ViewAuditLogs = ViewSettings,

    FullStudentAccess = ViewAllStudents | EditStudent | DeleteStudent | ViewStudentProgress |
                       ViewStudentQuestions | AnswerStudentQuestions | ManageAdminNotes |
                       SendNotifications | SendDirectEmail,

    FullAcademyAccess = ViewLevels | CreateLevel | EditLevel | DeleteLevel |
                       ViewAllCourses | CreateCourse | EditAllCourses | DeleteCourse | PublishCourse |
                       ViewLessons | CreateLesson | EditLesson | DeleteLesson |
                       ViewQuizzes | CreateQuiz | EditQuiz | DeleteQuiz | ViewQuizResults,

    FullQAAccess = ViewQA | CreateQA | EditQA | DeleteQA | ApproveQA,

    FullMuslimTubeAccess = ViewMuslimTube | CreateVideo | EditVideo | DeleteVideo | ApproveVideo,

    FullEventAccess = ViewEvents | CreateEvent | EditEvent | DeleteEvent | ManageEventRegistrations,

    // Admin has all permissions
    AllPermissions = ~None
}
