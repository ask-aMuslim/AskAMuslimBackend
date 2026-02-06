# Permission System - String-Based API

## Overview

The permission system has been enhanced to support **string-based permission names** in addition to numeric values. This provides better usability and flexibility for frontend applications while maintaining backward compatibility.

## Recent Changes

### 1. Fixed Permission Enum Overflow

**Problem**: The Permission enum had bits 64-69 defined, which exceeded the 64-bit capacity of the `long` type (bits 0-63 are valid).

**Solution**: Removed bits 64-69 and converted them to composite permissions using existing bits:

- `ManageSettings = ViewSettings` (composite)
- `ViewReports = ViewDashboard | ViewAnalytics` (composite)
- Other composite permissions: `FullStudentAccess`, `FullAcademyAccess`, `FullQAAccess`, etc.

### 2. Added String-Based Permission Support

**Enhancement**: Updated permission commands to accept permission names as strings instead of numeric values.

## API Usage

### Option 1: Using String Array (Recommended)

Send permission names as an array of strings:

```json
PUT /api/Permissions/{userId}
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "permissionNames": [
    "ViewDashboard",
    "ViewAnalytics",
    "ViewAllStudents",
    "CreateCourse",
    "EditOwnCourses"
  ],
  "notes": "Granted instructor permissions"
}
```

```json
PUT /api/Permissions/roles/{role}
{
  "role": "Instructor",
  "permissionNames": [
    "ViewDashboard",
    "ViewOwnCourses",
    "CreateCourse",
    "EditOwnCourses",
    "ViewEnrolledStudents"
  ],
  "notes": "Updated default instructor permissions"
}
```

### Option 2: Using Numeric Value (Backward Compatible)

For backward compatibility, you can still send the numeric Permission enum value:

```json
PUT /api/Permissions/{userId}
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "permissions": 127,
  "notes": "Granted permissions using numeric value"
}
```

## Getting Available Permissions

### Get All Permissions with Names

```http
GET /api/Permissions
```

**Response**: Returns all permissions grouped by category with their string names, display names, and numeric values:

```json
[
  {
    "category": "Dashboard",
    "permissions": [
      {
        "name": "ViewDashboard",
        "displayName": "View Dashboard",
        "category": "Dashboard",
        "value": 1
      },
      {
        "name": "ViewAnalytics",
        "displayName": "View Analytics",
        "category": "Dashboard",
        "value": 2
      }
    ]
  },
  {
    "category": "Admin Management",
    "permissions": [
      {
        "name": "ViewAdmins",
        "displayName": "View Admins",
        "category": "Admin Management",
        "value": 4
      }
      // ...more permissions
    ]
  }
  // ...more categories
]
```

### Get User Permissions

```http
GET /api/Permissions/{userId}
```

**Response**: Returns user's permissions in both formats:

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "instructor@example.com",
  "fullName": "John Doe",
  "roles": ["Instructor"],
  "permissions": 127, // Numeric value
  "permissionList": [
    // String names with metadata
    {
      "name": "ViewDashboard",
      "displayName": "View Dashboard",
      "category": "Dashboard",
      "value": 1,
      "isGranted": true
    },
    {
      "name": "ViewAnalytics",
      "displayName": "View Analytics",
      "category": "Dashboard",
      "value": 2,
      "isGranted": true
    }
    // ...more permissions
  ],
  "grantedDate": "2024-01-15T10:30:00Z",
  "grantedBy": "Admin User"
}
```

### Get Role Permissions

```http
GET /api/Permissions/roles/{role}
```

**Response**: Returns role's default permissions in both formats:

```json
{
  "role": "Instructor",
  "roleName": "Instructor",
  "permissions": 127, // Numeric value
  "permissionList": [
    // String names with metadata
    {
      "name": "ViewDashboard",
      "displayName": "View Dashboard",
      "category": "Dashboard",
      "value": 1,
      "isGranted": true
    }
    // ...more permissions
  ],
  "lastModifiedDate": "2024-01-15T10:30:00Z",
  "modifiedBy": "Admin User",
  "notes": "Updated default instructor permissions",
  "isCustomized": true
}
```

## Permission Categories

Permissions are organized into these categories:

- **Dashboard**: ViewDashboard, ViewAnalytics
- **Admin Management**: ViewAdmins, CreateAdmin, EditAdmin, DeleteAdmin, ManageAdminRoles
- **Student Management**: ViewAllStudents, ViewEnrolledStudents, EditStudent, DeleteStudent, etc.
- **Academy - Levels**: ViewLevels, CreateLevel, EditLevel, DeleteLevel
- **Academy - Courses**: ViewAllCourses, ViewOwnCourses, CreateCourse, EditOwnCourses, EditAllCourses, DeleteCourse, PublishCourse
- **Academy - Lessons**: ViewLessons, CreateLesson, EditLesson, DeleteLesson
- **Academy - Quizzes**: ViewQuizzes, CreateQuiz, EditQuiz, DeleteQuiz, ViewQuizResults
- **QA Management**: ViewQAs, CreateQA, EditQA, DeleteQA, ApproveQA, PublishQA, AnswerQA
- **MuslimTube**: ViewVideos, CreateVideo, EditOwnVideos, EditAllVideos, DeleteVideo, ManageChannels
- **Meeting & Inquiry Requests**: ViewMeetingRequests, ApproveMeetingRequests, ViewInquiryRequests, ApproveInquiryRequests
- **Events**: ViewEvents, CreateEvent, EditEvent, DeleteEvent
- **Tags**: ManageTags
- **Settings & System**: ViewSettings, ManageRolesAndPermissions, ViewFinancialReports, ManageBackups, ViewAuditLogs

## Composite Permissions

Composite permissions combine multiple permissions for convenience:

- **FullStudentAccess**: Combines all student management permissions
- **FullAcademyAccess**: Combines all academy permissions (courses, lessons, quizzes, levels)
- **FullQAAccess**: Combines all Q&A management permissions
- **FullMuslimTubeAccess**: Combines all MuslimTube permissions
- **FullEventAccess**: Combines all event management permissions
- **ManageSettings**: Alias for ViewSettings
- **ViewReports**: Combines ViewDashboard and ViewAnalytics

## Validation

The API validates permission names:

- If an invalid permission name is provided, the API returns a 400 Bad Request with details:

  ```json
  {
    "errors": ["Invalid permission name: InvalidPermissionName"]
  }
  ```

- Either `permissions` (numeric) OR `permissionNames` (array) must be provided:
  ```json
  {
    "errors": ["Either Permissions or PermissionNames must be provided"]
  }
  ```

## Best Practices

1. **Use String Names**: Prefer using `permissionNames` array for better readability and maintainability
2. **Check Available Permissions**: Always call `GET /api/Permissions` first to get the current list of available permissions
3. **Use Composite Permissions**: Use composite permissions (e.g., `FullAcademyAccess`) to grant multiple related permissions at once
4. **Case Sensitivity**: Permission names are case-insensitive (e.g., "viewdashboard", "ViewDashboard", "VIEWDASHBOARD" all work)
5. **Validate Names**: Always validate permission names against the available list to avoid errors

## Migration Guide

### Before (Numeric Only)

```json
PUT /api/Permissions/{userId}
{
  "userId": "...",
  "permissions": 127,  // Hard to understand what this means
  "notes": "..."
}
```

### After (String-Based)

```json
PUT /api/Permissions/{userId}
{
  "userId": "...",
  "permissionNames": [
    "ViewDashboard",
    "ViewAnalytics",
    "ViewAllStudents",
    "CreateCourse"
  ],  // Clear and readable
  "notes": "..."
}
```

## Technical Details

### Internal Storage

- Permissions are still stored as `long` (64-bit) flags in the database for efficiency
- Bitwise operations are used internally for permission checks
- String names are converted to enum values during command processing

### Command Structure

```csharp
public record UpdateUserPermissionsCommand : IRequest<Result>
{
    public Guid UserId { get; init; }
    public Permission? Permissions { get; init; }  // Optional: numeric value
    public List<string>? PermissionNames { get; init; }  // Optional: string names
    public string? Notes { get; init; }
}
```

### Conversion Process

1. Client sends permission names as strings
2. Command handler validates and converts each name to the Permission enum
3. Enum values are combined using bitwise OR
4. Combined value is stored in the database
5. GET queries expand the numeric value back to permission names for display
