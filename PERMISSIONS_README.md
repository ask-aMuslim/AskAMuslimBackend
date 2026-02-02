# Roles and Permissions System

## Overview

The application implements a comprehensive, flexible roles and permissions system that allows fine-grained access control for the admin panel. The system supports three admin roles: **Administrators**, **Instructors**, and **Preachers**.

## Key Features

- **Administrators have ALL permissions** by default
- **Custom permission assignment** - Admins can grant specific permissions to Instructors and Preachers
- **Granular permissions** - Separate permissions for View, Create, Edit, Delete actions
- **Category-based organization** - Permissions grouped by admin panel sections
- **Flag-based implementation** - Uses bitwise flags for efficient storage and checking

## Roles

### Administrator

- Has complete access to ALL features and pages
- Can manage other admins, instructors, and preachers
- Can assign custom permissions to other users
- Cannot have permissions revoked

### Instructor

- Default access to Academy features (Courses, Lessons, Quizzes)
- Default access to Student Management
- Can view enrolled students
- Can be granted additional permissions by Administrators

### Preacher

- Default access to QA Management
- Default access to MuslimTube
- Default access to Meeting & Inquiry Requests
- Default access to Events
- Can be granted additional permissions by Administrators

## Admin Panel Pages & Permissions

### 1. Dashboard

- `ViewDashboard` - View dashboard page
- `ViewAnalytics` - View analytics and statistics

### 2. Admins

- `ViewAdmins` - View list of admins
- `CreateAdmin` - Create new admin users
- `EditAdmin` - Edit admin profiles
- `DeleteAdmin` - Delete admin users
- `ManageAdminRoles` - Assign/change admin roles

### 3. Students

- `ViewAllStudents` - View all students
- `ViewEnrolledStudents` - View students enrolled in courses
- `EditStudent` - Edit student information
- `DeleteStudent` - Delete student accounts
- `ViewStudentProgress` - View student course progress
- `ViewStudentQuestions` - View student questions
- `AnswerStudentQuestions` - Answer student questions
- `ManageAdminNotes` - Manage notes about students
- `SendNotifications` - Send notifications to students
- `SendDirectEmail` - Send direct emails to students

### 4. Academy

#### Levels

- `ViewLevels` - View course levels
- `CreateLevel` - Create new levels
- `EditLevel` - Edit existing levels
- `DeleteLevel` - Delete levels

#### Courses

- `ViewAllCourses` - View all courses
- `ViewOwnCourses` - View own courses only
- `CreateCourse` - Create new courses
- `EditOwnCourses` - Edit own courses
- `EditAllCourses` - Edit any course
- `DeleteCourse` - Delete courses
- `PublishCourse` - Publish/unpublish courses

#### Lessons

- `ViewLessons` - View lessons
- `CreateLesson` - Create new lessons
- `EditLesson` - Edit lessons
- `DeleteLesson` - Delete lessons

#### Quizzes

- `ViewQuizzes` - View quizzes
- `CreateQuiz` - Create new quizzes
- `EditQuiz` - Edit quizzes
- `DeleteQuiz` - Delete quizzes
- `ViewQuizResults` - View quiz results and attempts

### 5. QA Management

- `ViewQA` - View Q&A entries
- `CreateQA` - Create new Q&A entries
- `EditQA` - Edit Q&A entries
- `DeleteQA` - Delete Q&A entries
- `ApproveQA` - Approve/publish Q&A entries

### 6. MuslimTube

- `ViewMuslimTube` - View videos
- `CreateVideo` - Upload new videos
- `EditVideo` - Edit video details
- `DeleteVideo` - Delete videos
- `ApproveVideo` - Approve/publish videos

### 7. Notifications

- `ViewNotifications` - View notifications
- `SendBulkNotifications` - Send bulk notifications
- `CreateNotificationTemplate` - Create notification templates

### 8. Meeting & Inquiry Requests

- `ViewMeetingRequests` - View meeting requests
- `ManageMeetingRequests` - Manage meeting requests
- `ViewInquiryRequests` - View inquiry requests
- `ManageInquiryRequests` - Manage inquiry requests

### 9. Events

- `ViewEvents` - View events
- `CreateEvent` - Create new events
- `EditEvent` - Edit events
- `DeleteEvent` - Delete events
- `ManageEventRegistrations` - Manage event registrations

### 10. Tags

- `ViewTags` - View tags
- `CreateTag` - Create new tags
- `EditTag` - Edit tags
- `DeleteTag` - Delete tags

### 11. Settings & System

- `ViewSettings` - View system settings
- `ManageSettings` - Modify system settings
- `ViewReports` - View system reports
- `ManageRolesAndPermissions` - Manage user roles and permissions
- `ViewFinancialData` - View financial information
- `ManageSystemBackup` - Manage system backups
- `ViewAuditLogs` - View audit logs

## API Endpoints

### Get All Available Permissions

```http
GET /api/Permissions
Authorization: Required (Administrator only)
```

Returns all available permissions grouped by category.

### Get User Permissions

```http
GET /api/Permissions/{userId}
Authorization: Required (Administrator only)
```

Returns the permissions assigned to a specific user.

### Update User Permissions

```http
PUT /api/Permissions/{userId}
Authorization: Required (Administrator only)
Content-Type: application/json

{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "permissions": 2199023779840,  // Combined flag values
  "notes": "Granted access to course management"
}
```

## How It Works

### 1. Default Role-Based Access

When a user has a role (Instructor, Preacher), they automatically get default permissions for that role as defined in `DependencyInjection.cs`.

### 2. Custom Permission Override

Administrators can grant specific permissions to users through the `UserPermissions` table. These custom permissions are checked FIRST before falling back to role-based permissions.

### 3. Administrator Always Has Access

The system checks if a user has the Administrator role FIRST. If yes, access is always granted regardless of custom permissions.

### 4. Permission Checking Flow

```
1. Check if user is Administrator → Grant access
2. Check custom permissions in UserPermissions table → Grant if permission flag is set
3. Check role-based default permissions → Grant if role has access
4. Deny access
```

## Usage Examples

### Example 1: Grant Instructor Full QA Access

```csharp
var permissions = Permission.ViewQA | Permission.CreateQA |
                 Permission.EditQA | Permission.DeleteQA | Permission.ApproveQA;

await sender.Send(new UpdateUserPermissionsCommand
{
    UserId = instructorId,
    Permissions = permissions,
    Notes = "Granted full QA management access"
});
```

### Example 2: Grant Preacher Course Creation

```csharp
var permissions = Permission.ViewAllCourses | Permission.CreateCourse |
                 Permission.EditOwnCourses;

await sender.Send(new UpdateUserPermissionsCommand
{
    UserId = preacherId,
    Permissions = permissions,
    Notes = "Can create and manage own courses"
});
```

### Example 3: Using Composite Permissions

```csharp
// Grant full academy access at once
var permissions = Permission.FullAcademyAccess;

await sender.Send(new UpdateUserPermissionsCommand
{
    UserId = userId,
    Permissions = permissions
});
```

## Frontend Integration

The frontend should:

1. **Check permissions before showing UI elements**
   - Hide/disable buttons user doesn't have permission for
   - Don't show menu items for inaccessible pages

2. **Use the permission API**

   ```javascript
   // Get all available permissions
   GET / api / Permissions;

   // Get user's current permissions
   GET / api / Permissions / { userId };

   // Update permissions (admin only)
   PUT / api / Permissions / { userId };
   ```

3. **Display permissions by category**
   - Group checkboxes by category for easy management
   - Show clear permission names (from DisplayName)

4. **Handle permission combinations**
   - Use bitwise operations for combining permissions
   - Check individual flags using bitwise AND

## Database Schema

### UserPermissions Table

| Column          | Type     | Description                   |
| --------------- | -------- | ----------------------------- |
| Id              | Guid     | Primary key                   |
| UserId          | Guid     | User ID (FK to Users)         |
| Permissions     | long     | Bitwise flag combination      |
| GrantedDate     | DateTime | When permissions were granted |
| GrantedByUserId | Guid?    | Who granted the permissions   |
| Notes           | string?  | Optional notes                |

## Important Notes

1. **Administrators cannot have permissions restricted** - They always have full access
2. **Permission values are cumulative** - Use bitwise OR to combine multiple permissions
3. **Custom permissions override role defaults** - If a UserPermission record exists, it takes precedence
4. **Audit trail** - System tracks who granted permissions and when
5. **Future-proof** - Permission enum supports up to 70 distinct permissions using long (64-bit)

## Migration Notes

If you need to add new permissions:

1. Add to `Permission` enum in `Domain/Enums/Permission.cs`
2. Add to `Policies` constants in `Domain/Constants/Policies.cs`
3. Add policy mapping in `Infrastructure/DependencyInjection.cs`
4. Add permission checking in `IdentityService.AuthorizeAsync` and `CheckPermissionForPolicy`
5. Add to permission list in `GetAllPermissionsQuery.cs`
