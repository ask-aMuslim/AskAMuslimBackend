# User Management System Documentation

## Overview

This document describes the custom user management system for AskAMuslimBackend, which provides role-specific management for **Students**, **Instructors**, and **Admins** following the CQRS pattern.

## Architecture

### Domain Layer

Three new entities were created to manage role-specific user data:

#### 1. Student Entity (`src/Domain/Entities/Student.cs`)

```csharp
- UserId (string) - Link to ApplicationUser
- Bio (string?)
- PhoneNumber (string?)
- Address (string?)
- DateOfBirth (DateTime?)
- EnrollmentDate (DateTime) - Auto-set on creation
- IsActive (bool) - Default: true
- Enrollments (Collection)
- StudentNotes (Collection)
- StudentQuestions (Collection)
```

#### 2. Instructor Entity (`src/Domain/Entities/Instructor.cs`)

```csharp
- UserId (string) - Link to ApplicationUser
- Bio (string?)
- Specialization (string?)
- PhoneNumber (string?)
- Website (string?)
- LinkedInProfile (string?)
- YearsOfExperience (int)
- IsVerified (bool) - Default: false
- IsActive (bool) - Default: true
- JoinedDate (DateTime)
- Rating (decimal)
- TotalReviews (int)
- Courses (Collection)
```

#### 3. Admin Entity (`src/Domain/Entities/Admin.cs`)

```csharp
- UserId (string) - Link to ApplicationUser
- Department (string?)
- PhoneNumber (string?)
- IsSuperAdmin (bool) - Default: false
- IsActive (bool) - Default: true
- JoinedDate (DateTime)
- LastLoginDate (DateTime?)
- CanManageUsers (bool) - Default: true
- CanManageCourses (bool) - Default: true
- CanManageContent (bool) - Default: true
- CanViewReports (bool) - Default: true
- CanManageRoles (bool) - Default: false
```

## API Endpoints

### Student Management

#### Get My Student Profile

**Endpoint:** `GET /students/me`  
**Authorization:** Required (Student role)

**Response:**

```json
{
  "id": "guid",
  "userId": "string",
  "email": "student@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "bio": "Student bio...",
  "phoneNumber": "+1234567890",
  "address": "123 Main St",
  "dateOfBirth": "2000-01-15T00:00:00Z",
  "enrollmentDate": "2024-01-01T00:00:00Z",
  "isActive": true,
  "totalEnrollments": 5
}
```

#### Get Student Profile by User ID

**Endpoint:** `GET /students/{userId}`  
**Authorization:** Required

**Parameters:**

- `userId` - The ApplicationUser ID

#### Get All Students

**Endpoint:** `GET /students`  
**Authorization:** Required (Admin only recommended)

**Response:** Array of StudentDto objects

#### Update Student Profile

**Endpoint:** `PUT /students/me`  
**Authorization:** Required (Student role)

**Request Body:**

```json
{
  "bio": "Updated bio",
  "phoneNumber": "+1234567890",
  "address": "New address",
  "dateOfBirth": "2000-01-15T00:00:00Z"
}
```

---

### Instructor Management

#### Get My Instructor Profile

**Endpoint:** `GET /instructors/me`  
**Authorization:** Required (Instructor role)

**Response:**

```json
{
  "id": "guid",
  "userId": "string",
  "email": "instructor@example.com",
  "firstName": "Bob",
  "lastName": "Smith",
  "bio": "Experienced instructor...",
  "specialization": "Islamic Studies",
  "phoneNumber": "+1234567890",
  "website": "https://example.com",
  "linkedInProfile": "https://linkedin.com/in/example",
  "yearsOfExperience": 10,
  "isVerified": true,
  "isActive": true,
  "joinedDate": "2023-01-01T00:00:00Z",
  "rating": 4.8,
  "totalReviews": 120,
  "totalCourses": 15
}
```

#### Get Instructor Profile by User ID

**Endpoint:** `GET /instructors/{userId}`  
**Authorization:** Required

#### Get All Instructors

**Endpoint:** `GET /instructors`  
**Authorization:** None (Public endpoint)

**Response:** Array of active InstructorDto objects

#### Update Instructor Profile

**Endpoint:** `PUT /instructors/me`  
**Authorization:** Required (Instructor role)

**Request Body:**

```json
{
  "bio": "Updated bio",
  "specialization": "Quran & Hadith",
  "phoneNumber": "+1234567890",
  "website": "https://mywebsite.com",
  "linkedInProfile": "https://linkedin.com/in/myprofile",
  "yearsOfExperience": 12
}
```

---

### Admin Management

#### Get My Admin Profile

**Endpoint:** `GET /admins/me`  
**Authorization:** Required (Admin role)

**Response:**

```json
{
  "id": "guid",
  "userId": "string",
  "email": "admin@example.com",
  "firstName": "Admin",
  "lastName": "User",
  "department": "IT",
  "phoneNumber": "+1234567890",
  "isSuperAdmin": false,
  "isActive": true,
  "joinedDate": "2023-01-01T00:00:00Z",
  "lastLoginDate": "2025-11-07T10:30:00Z",
  "canManageUsers": true,
  "canManageCourses": true,
  "canManageContent": true,
  "canViewReports": true,
  "canManageRoles": false
}
```

#### Get Admin Profile by User ID

**Endpoint:** `GET /admins/{userId}`  
**Authorization:** Required (Admin role)

#### Update Admin Profile

**Endpoint:** `PUT /admins/me`  
**Authorization:** Required (Admin role)

**Request Body:**

```json
{
  "department": "Operations",
  "phoneNumber": "+1234567890"
}
```

---

## Registration Flow

When a user registers through `/authentication/register`, the system automatically:

1. Creates an `ApplicationUser` record in Identity
2. Assigns the appropriate role (Admin, Student, Instructor, NonMuslim)
3. Creates a corresponding role-specific entity:
   - **Admin** → Creates `Admin` entity with default permissions
   - **Student** → Creates `Student` entity
   - **Instructor** → Creates `Instructor` entity
   - **NonMuslim** → Creates `Student` entity (treated as students)

This happens automatically in the `IdentityService.RegisterUserAsync` method.

## Seeded Data

The system automatically seeds test users with their corresponding role entities:

| Role       | Email                   | Password        | Entity Created |
| ---------- | ----------------------- | --------------- | -------------- |
| Admin      | administrator@localhost | Administrator1! | Admin          |
| Student    | student@localhost       | Student1!       | Student        |
| Instructor | instructor@localhost    | Instructor1!    | Instructor     |
| NonMuslim  | nonmuslim@localhost     | NonMuslim1!     | Student        |

## Validation Rules

### Student Profile Update

- **PhoneNumber**: Max 20 characters
- **Address**: Max 500 characters
- **Bio**: Max 1000 characters
- **DateOfBirth**: Must be in the past

### Instructor Profile Update

- **PhoneNumber**: Max 20 characters
- **Bio**: Max 2000 characters
- **Specialization**: Max 200 characters
- **Website**: Max 500 characters
- **LinkedInProfile**: Max 500 characters
- **YearsOfExperience**: 0-100

### Admin Profile Update

- **Department**: Max 200 characters
- **PhoneNumber**: Max 20 characters

## CQRS Implementation

### Student Management

**Commands:**

- `UpdateStudentProfileCommand` - Update student profile

**Queries:**

- `GetStudentProfileQuery` - Get student profile (own or by userId)
- `GetAllStudentsQuery` - Get all students

**Location:** `src/Application/Students/`

### Instructor Management

**Commands:**

- `UpdateInstructorProfileCommand` - Update instructor profile

**Queries:**

- `GetInstructorProfileQuery` - Get instructor profile (own or by userId)
- `GetAllInstructorsQuery` - Get all active instructors

**Location:** `src/Application/Instructors/`

### Admin Management

**Commands:**

- `UpdateAdminProfileCommand` - Update admin profile

**Queries:**

- `GetAdminProfileQuery` - Get admin profile (own or by userId)

**Location:** `src/Application/Admins/`

## Database Schema

The entities are stored in separate tables:

- `Students` - Student-specific data
- `Instructors` - Instructor-specific data
- `Admins` - Admin-specific data

All linked to `AspNetUsers` (ApplicationUser) via `UserId` foreign key.

## Security Considerations

1. **Authorization**: All endpoints require authentication except `GET /instructors` (public)
2. **User Context**: Users can only update their own profiles
3. **Admin Permissions**: Admin entity has granular permission flags for fine-grained access control
4. **Instructor Verification**: `IsVerified` flag can be used to approve instructors before they can create courses

## Usage Examples

### Get Your Student Profile (C#)

```csharp
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

var response = await httpClient.GetAsync("/students/me");
var profile = await response.Content.ReadFromJsonAsync<StudentDto>();
```

### Update Instructor Profile

```csharp
var updateCommand = new UpdateInstructorProfileCommand
{
    Bio = "10+ years teaching Islamic Studies",
    Specialization = "Quran & Tafseer",
    YearsOfExperience = 12
};

var response = await httpClient.PutAsJsonAsync("/instructors/me", updateCommand);
```

### List All Instructors (No Auth Required)

```csharp
var response = await httpClient.GetAsync("/instructors");
var instructors = await response.Content.ReadFromJsonAsync<List<InstructorDto>>();
```

## Testing

1. Register a new user with specific role
2. Login to get JWT token
3. Use token to access role-specific endpoints
4. Update profile and verify changes
5. Test cross-role access (should fail appropriately)

## Future Enhancements

Consider adding:

- [ ] Profile picture upload
- [ ] Instructor approval workflow (Admin approves new instructors)
- [ ] Student progress dashboard
- [ ] Admin analytics and reporting
- [ ] Instructor rating and review system
- [ ] Bulk user management (Admin only)
- [ ] User search and filtering
- [ ] Activity logs and audit trails
- [ ] Export user data functionality
