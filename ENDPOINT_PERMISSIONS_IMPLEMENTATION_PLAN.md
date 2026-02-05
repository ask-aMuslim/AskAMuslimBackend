# Endpoint Permissions Implementation Plan

## 📋 Overview

This document outlines the comprehensive plan for implementing permission checks across all API endpoints in the AskAMuslimBackend application. The goal is to ensure every endpoint has appropriate authorization requirements based on the existing roles and permissions system.

**Date Created:** February 6, 2026  
**Status:** Planning Phase  
**Estimated Effort:** 30-40 hours  
**Priority:** High (Security Critical)

---

## 🎯 Objectives

1. **Security**: Ensure all admin/protected endpoints have proper permission checks
2. **Consistency**: Apply uniform authorization patterns across the application
3. **Granularity**: Use specific permission policies rather than generic `.RequireAuthorization()`
4. **Documentation**: Update endpoint summaries to reflect required permissions

---

## 📊 Current State Analysis

### Endpoint Inventory (30 Files)

| Endpoint File         | Estimated Routes | Current Auth Status     |
| --------------------- | ---------------- | ----------------------- |
| AdminNotes.cs         | 4-5              | ⚠️ Needs Review         |
| Admins.cs             | 4                | ✅ Partially Complete   |
| Authentication.cs     | 3-5              | ✅ Public/Auth Only     |
| Certificates.cs       | 3-4              | ⚠️ Needs Review         |
| Courses.cs            | 9                | ❌ Mixed (Some missing) |
| Enrollments.cs        | 4-5              | ⚠️ Needs Review         |
| EventRegistrations.cs | 3-4              | ⚠️ Needs Review         |
| Events.cs             | 5-6              | ❌ Missing Policies     |
| InquiryRequests.cs    | 4-5              | ⚠️ Needs Review         |
| Instructors.cs        | 4-5              | ⚠️ Needs Review         |
| Lessons.cs            | 5-6              | ❌ Missing Policies     |
| Levels.cs             | 5                | ⚠️ Needs Review         |
| Media.cs              | 3-4              | ⚠️ Needs Review         |
| MeetingRequests.cs    | 4-5              | ⚠️ Needs Review         |
| Moderators.cs         | 3-4              | ⚠️ Needs Review         |
| MuslimTube.cs         | 5-6              | ❌ Missing Policies     |
| Notifications.cs      | 3-4              | ⚠️ Needs Review         |
| Options.cs            | 3-4              | ⚠️ Needs Review         |
| Permissions.cs        | 6                | ✅ Complete             |
| Preachers.cs          | 4-5              | ⚠️ Needs Review         |
| QAs.cs                | 5                | ❌ Missing Policies     |
| Questions.cs          | 4-5              | ⚠️ Needs Review         |
| QuizAttempts.cs       | 3-4              | ⚠️ Needs Review         |
| Quizzes.cs            | 5-6              | ❌ Missing Policies     |
| Statistics.cs         | 3-5              | ⚠️ Needs Review         |
| StudentNotes.cs       | 4-5              | ⚠️ Needs Review         |
| StudentProfiles.cs    | 3-4              | ⚠️ Needs Review         |
| StudentQuestions.cs   | 4-5              | ⚠️ Needs Review         |
| Students.cs           | 4                | ✅ Partially Complete   |
| Tags.cs               | 5                | ⚠️ Needs Review         |

**Legend:**

- ✅ Complete: All endpoints have appropriate policies
- ⚠️ Needs Review: Has some authorization but needs verification/updates
- ❌ Missing Policies: Has generic auth or no specific policies

---

## 🗺️ Permission Mapping Strategy

### Endpoint to Policy Mapping Guide

#### 1. **AdminNotes** → `ManageAdminNotes`

- GET (All) → `ManageAdminNotes`
- GET (By ID) → `ManageAdminNotes`
- POST (Create) → `ManageAdminNotes`
- PUT (Update) → `ManageAdminNotes`
- DELETE → `ManageAdminNotes`

#### 2. **Admins** → `ViewAdmins`, `ManageAdmins`

- ✅ GET /me → `.RequireAuthorization()` (any admin)
- ✅ GET /{userId} → `ViewAdmins`
- ✅ GET (All) → `ViewAdmins`
- ✅ PUT /me → `.RequireAuthorization()` (any admin)
- ⚠️ POST (Create) → `ManageAdmins` (if exists)
- ⚠️ DELETE → `ManageAdmins` (if exists)

#### 3. **Authentication** → Public/Basic Auth

- POST /register → Public
- POST /login → Public
- POST /refresh → Public or `.RequireAuthorization()`
- POST /logout → `.RequireAuthorization()`

#### 4. **Certificates** → Student Feature

- GET /my-certificates → `.RequireAuthorization()` (Student)
- GET /{id} → `.RequireAuthorization()` (Student) or Public
- POST /generate → Admin only (needs specific policy)

#### 5. **Courses** → `ViewAllCourses`, `ManageAllCourses`, `ManageOwnCourses`

- GET (List) → Public or `ViewAllCourses` (depends on visibility)
- GET /{id} → Public or `ViewAllCourses`
- GET /{id}/detail → Public or `ViewAllCourses`
- GET roadmap → Public
- POST (Create) → `CreateCourse`
- PUT (Update) → `ManageOwnCourses` or `ManageAllCourses`
- DELETE → `ManageAllCourses`
- POST /prerequisites → `ManageAllCourses`
- DELETE /prerequisites → `ManageAllCourses`

#### 6. **Enrollments** → Student + Admin

- GET /my-enrollments → `.RequireAuthorization()` (Student)
- GET (All) → `ViewEnrolledStudents`
- POST (Enroll) → `.RequireAuthorization()` (Student)
- DELETE (Unenroll) → `.RequireAuthorization()` (Student) or Admin

#### 7. **EventRegistrations** → `ManageEventRegistrations`

- GET (All) → `ManageEventRegistrations`
- GET /{id} → `ManageEventRegistrations`
- POST → `.RequireAuthorization()` (Student)
- DELETE → `ManageEventRegistrations`

#### 8. **Events** → `ViewEvents`, `ManageEvents`

- GET (List) → Public or `ViewEvents`
- GET /{id} → Public or `ViewEvents`
- POST (Create) → `ManageEvents`
- PUT (Update) → `ManageEvents`
- DELETE → `ManageEvents`

#### 9. **InquiryRequests** → `ViewInquiryRequests`, `ManageInquiryRequests`

- GET (All) → `ViewInquiryRequests`
- GET /{id} → `ViewInquiryRequests`
- POST (Submit) → `.RequireAuthorization()` (Student) or Public
- PUT (Update/Respond) → `ManageInquiryRequests`
- DELETE → `ManageInquiryRequests`

#### 10. **Instructors** → `ViewAdmins`, `ManageAdmins`

- GET (All) → `ViewAdmins`
- GET /{id} → `ViewAdmins`
- POST (Create) → `ManageAdmins`
- PUT (Update) → `ManageAdmins`
- DELETE → `ManageAdmins`

#### 11. **Lessons** → `ViewLessons`, `ManageLessons`

- GET (List) → Public or `ViewLessons`
- GET /{id} → `.RequireAuthorization()` (Student enrolled)
- POST (Create) → `ManageLessons`
- PUT (Update) → `ManageLessons`
- DELETE → `ManageLessons`

#### 12. **Levels** → `ViewLevels`, `ManageLevels`

- GET (List) → Public or `ViewLevels`
- GET /{id} → Public or `ViewLevels`
- POST (Create) → `ManageLevels`
- PUT (Update) → `ManageLevels`
- DELETE → `ManageLevels`

#### 13. **Media** → Based on context

- POST /upload → `.RequireAuthorization()`
- DELETE → `.RequireAuthorization()`
- GET → Public (if used for public content)

#### 14. **MeetingRequests** → `ViewMeetingRequests`, `ManageMeetingRequests`

- GET (All) → `ViewMeetingRequests`
- GET /{id} → `ViewMeetingRequests`
- POST (Submit) → `.RequireAuthorization()` (Student)
- PUT (Update/Respond) → `ManageMeetingRequests`
- DELETE → `ManageMeetingRequests`

#### 15. **Moderators** → `ViewAdmins`, `ManageAdmins` (or specific moderator policies)

- GET (All) → `ViewAdmins`
- GET /{id} → `ViewAdmins`
- POST (Create) → `ManageAdmins`
- PUT (Update) → `ManageAdmins`
- DELETE → `ManageAdmins`

#### 16. **MuslimTube** → `ViewMuslimTube`, `ManageMuslimTube`, `ApproveVideos`

- GET (List) → Public or `ViewMuslimTube`
- GET /{id} → Public or `ViewMuslimTube`
- POST (Create) → `ManageMuslimTube`
- PUT (Update) → `ManageMuslimTube`
- DELETE → `ManageMuslimTube`
- PUT /approve → `ApproveVideos`

#### 17. **Notifications** → `ViewNotifications`, `SendBulkNotifications`

- GET /my-notifications → `.RequireAuthorization()`
- GET (All admin) → `ViewNotifications`
- POST (Send) → `SendBulkNotifications`

#### 18. **Options** → Based on quiz/question context

- Generally tied to quiz management

#### 19. **Permissions** → `ManageRolesAndPermissions`

- ✅ All endpoints → `ManageRolesAndPermissions`

#### 20. **Preachers** → `ViewAdmins`, `ManageAdmins`

- Similar to Instructors

#### 21. **QAs** → `ViewQA`, `ManageQA`, `ApproveQA`

- GET (List) → Public or `ViewQA`
- GET /{id} → Public or `ViewQA`
- POST (Create) → `ManageQA`
- PUT (Update) → `ManageQA`
- DELETE → `ManageQA`
- PUT /approve → `ApproveQA`

#### 22. **Questions** → Related to StudentQuestions

- Context-dependent

#### 23. **QuizAttempts** → Student + Admin

- GET /my-attempts → `.RequireAuthorization()` (Student)
- GET (All) → `ViewQuizzes` or `ManageQuizzes`
- POST (Submit) → `.RequireAuthorization()` (Student)

#### 24. **Quizzes** → `ViewQuizzes`, `ManageQuizzes`

- GET (List) → Public or `ViewQuizzes`
- GET /{id} → `.RequireAuthorization()` (Student enrolled)
- POST (Create) → `ManageQuizzes`
- PUT (Update) → `ManageQuizzes`
- DELETE → `ManageQuizzes`

#### 25. **Statistics** → `ViewDashboard`, `ViewAnalytics`

- GET /dashboard → `ViewDashboard`
- GET /analytics → `ViewAnalytics`
- GET /reports → `ViewReports`

#### 26. **StudentNotes** → Student feature

- GET /my-notes → `.RequireAuthorization()` (Student)
- POST → `.RequireAuthorization()` (Student)
- PUT → `.RequireAuthorization()` (Student)
- DELETE → `.RequireAuthorization()` (Student)

#### 27. **StudentProfiles** → Student feature

- GET /me → `.RequireAuthorization()` (Student)
- PUT /me → `.RequireAuthorization()` (Student)
- GET /{id} → `ViewAllStudents` (Admin view)

#### 28. **StudentQuestions** → Student + Instructor

- GET /my-questions → `.RequireAuthorization()` (Student)
- GET (All) → `ViewStudentQuestions` (Admin/Instructor)
- POST → `.RequireAuthorization()` (Student)
- PUT /answer → `AnswerStudentQuestions`

#### 29. **Students** → `ViewAllStudents`, `ManageStudents`

- ✅ GET /{userId} → `.RequireAuthorization()`
- ✅ GET (All) → `.RequireAuthorization()` (needs `ViewAllStudents`)
- ✅ PUT /me → `.RequireAuthorization()` (Student)
- ✅ PUT /{id}/status → `ManageStudents`

#### 30. **Tags** → `ViewTags`, `ManageTags`

- GET (List) → Public or `ViewTags`
- GET /{id} → Public or `ViewTags`
- POST (Create) → `ManageTags`
- PUT (Update) → `ManageTags`
- DELETE → `ManageTags`

---

## 📦 Implementation Batches

### **Batch 1: High Priority Admin Management** (4-6 hours)

**Files:** 5 files  
**Focus:** Admin user management and permissions

- [ ] `Admins.cs` - Review and add missing policies
- [ ] `Instructors.cs` - Add `ViewAdmins`, `ManageAdmins`
- [ ] `Preachers.cs` - Add `ViewAdmins`, `ManageAdmins`
- [ ] `Moderators.cs` - Add appropriate policies
- [ ] `Permissions.cs` - ✅ Already complete, verify only

**Testing Focus:**

- Only admins with proper permissions can view/manage other admins
- Role-based restrictions work correctly

---

### **Batch 2: Student Management** (4-6 hours)

**Files:** 6 files  
**Focus:** Student-related endpoints and data

- [ ] `Students.cs` - Update GET all to use `ViewAllStudents`
- [ ] `StudentProfiles.cs` - Add view permissions for admin access
- [ ] `StudentQuestions.cs` - Add `ViewStudentQuestions`, `AnswerStudentQuestions`
- [ ] `StudentNotes.cs` - Verify student-only access
- [ ] `AdminNotes.cs` - Add `ManageAdminNotes`
- [ ] `Notifications.cs` - Add `ViewNotifications`, `SendBulkNotifications`

**Testing Focus:**

- Students can only access their own data
- Admins need proper permissions to view student data
- Instructors can view enrolled students only

---

### **Batch 3: Academy - Core Learning** (6-8 hours)

**Files:** 6 files  
**Focus:** Course, lesson, and quiz management

- [ ] `Levels.cs` - Add `ViewLevels`, `ManageLevels`
- [ ] `Courses.cs` - Add granular policies (ViewAll, ManageAll, ManageOwn)
- [ ] `Lessons.cs` - Add `ViewLessons`, `ManageLessons`
- [ ] `Quizzes.cs` - Add `ViewQuizzes`, `ManageQuizzes`
- [ ] `Options.cs` - Add quiz management policies
- [ ] `Questions.cs` - Add quiz management policies

**Testing Focus:**

- Public can view course catalog
- Enrolled students can access lessons and quizzes
- Instructors can manage own courses
- Admins can manage all courses

---

### **Batch 4: Academy - Student Progress** (3-4 hours)

**Files:** 3 files  
**Focus:** Enrollments, certificates, and quiz attempts

- [ ] `Enrollments.cs` - Add student + admin policies
- [ ] `Certificates.cs` - Add appropriate policies
- [ ] `QuizAttempts.cs` - Add student + admin view policies

**Testing Focus:**

- Students can enroll in courses
- Students can view their own progress
- Admins can view student progress with proper permissions

---

### **Batch 5: Content Management - QA & Media** (4-5 hours)

**Files:** 3 files  
**Focus:** Q&A, MuslimTube, and media

- [ ] `QAs.cs` - Add `ViewQA`, `ManageQA`, `ApproveQA`
- [ ] `MuslimTube.cs` - Add `ViewMuslimTube`, `ManageMuslimTube`, `ApproveVideos`
- [ ] `Media.cs` - Add appropriate upload/delete policies

**Testing Focus:**

- Public can view approved content
- Preachers can manage QA and MuslimTube
- Approval workflow requires proper permissions

---

### **Batch 6: Community Features** (4-5 hours)

**Files:** 4 files  
**Focus:** Events, meetings, and inquiries

- [ ] `Events.cs` - Add `ViewEvents`, `ManageEvents`
- [ ] `EventRegistrations.cs` - Add `ManageEventRegistrations`
- [ ] `MeetingRequests.cs` - Add `ViewMeetingRequests`, `ManageMeetingRequests`
- [ ] `InquiryRequests.cs` - Add `ViewInquiryRequests`, `ManageInquiryRequests`

**Testing Focus:**

- Public/students can submit requests
- Preachers can manage meeting/inquiry requests
- Event management requires proper permissions

---

### **Batch 7: System & Utilities** (3-4 hours)

**Files:** 3 files  
**Focus:** Tags, statistics, and authentication

- [ ] `Tags.cs` - Add `ViewTags`, `ManageTags`
- [ ] `Statistics.cs` - Add `ViewDashboard`, `ViewAnalytics`, `ViewReports`
- [ ] `Authentication.cs` - Verify public/auth-only endpoints

**Testing Focus:**

- Analytics restricted to admins with ViewDashboard/ViewAnalytics
- Public authentication works correctly
- Tag management restricted appropriately

---

## 🔧 Implementation Guidelines

### Standard Patterns

#### 1. **Public Endpoints** (No Authentication Required)

```csharp
groupBuilder.MapGet(GetPublicData)
    .WithName("GetPublicData")
    .WithSummary("Get public data (no authentication required)");
```

#### 2. **Authenticated Endpoints** (Any Authenticated User)

```csharp
groupBuilder.MapGet(GetMyData)
    .RequireAuthorization()
    .WithName("GetMyData")
    .WithSummary("Get current user's data");
```

#### 3. **Permission-Based Endpoints** (Specific Permission Required)

```csharp
groupBuilder.MapGet(GetAllAdmins)
    .RequireAuthorization(Policies.ViewAdmins)
    .WithName("GetAllAdmins")
    .WithSummary("Get all admins (requires ViewAdmins permission)");
```

#### 4. **Multiple Permissions** (If needed, create combined policy)

```csharp
// In DependencyInjection.cs
services.AddAuthorization(options =>
{
    options.AddPolicy("ViewOrManageCourses", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "permission" &&
                (c.Value == "ViewAllCourses" || c.Value == "ManageAllCourses"))));
});
```

### Permission Selection Logic

1. **Is the endpoint public?** → No authorization
2. **Is it user-specific data?** → `.RequireAuthorization()` only
3. **Is it admin-only data/action?** → Use specific permission policy
4. **Does ownership matter?** → Consider ManageOwn vs ManageAll policies

### Update Checklist for Each Endpoint

- [ ] Add appropriate `.RequireAuthorization()` or `.RequireAuthorization(Policies.XYZ)`
- [ ] Update `.WithSummary()` to include permission requirement
- [ ] Consider public vs authenticated vs permission-based access
- [ ] Document any assumptions or special cases

---

## 🧪 Testing Strategy

### Test Categories

#### 1. **Unauthorized Access Tests**

- Verify unauthenticated users cannot access protected endpoints
- Expect: 401 Unauthorized

#### 2. **Forbidden Access Tests**

- Verify authenticated users without proper permissions cannot access
- Expect: 403 Forbidden

#### 3. **Authorized Access Tests**

- Verify users with correct permissions can access
- Expect: 200 OK (or appropriate success response)

#### 4. **Role-Based Tests**

- Administrators: Should have access to everything
- Instructors: Should have default academy + assigned permissions
- Preachers: Should have default QA/Events + assigned permissions
- Students: Should have access to their own data only

### Testing Tools

1. **Web.http file** - Manual testing during development
2. **Swagger UI** - Visual testing and documentation
3. **Unit Tests** - Automated authorization tests (if time permits)
4. **Integration Tests** - End-to-end authorization flow tests

---

## 📝 Documentation Updates

### Files to Update After Implementation

1. **PERMISSIONS_README.md**
   - Update with any new permissions discovered
   - Add endpoint-to-permission mapping table

2. **API Documentation**
   - Update Swagger summaries
   - Add permission requirements to endpoint descriptions

3. **This Plan Document**
   - Mark batches as complete
   - Document any deviations or discoveries

---

## ✅ Completion Criteria

### Definition of Done

- [ ] All 30 endpoint files reviewed
- [ ] All admin endpoints have appropriate permission policies
- [ ] All student endpoints have proper authentication
- [ ] Public endpoints explicitly identified (no unintended public access)
- [ ] All endpoint summaries updated with permission requirements
- [ ] Manual testing completed for each batch
- [ ] Documentation updated
- [ ] No security vulnerabilities introduced
- [ ] Code review completed

### Success Metrics

1. **Coverage**: 100% of endpoints have explicit authorization decision
2. **Security**: Zero unprotected admin endpoints
3. **Consistency**: All similar operations use similar policies
4. **Documentation**: All endpoints documented with permission requirements

---

## 🚨 Risk Assessment & Mitigation

### Potential Risks

| Risk                              | Impact | Likelihood | Mitigation                                                   |
| --------------------------------- | ------ | ---------- | ------------------------------------------------------------ |
| Breaking existing functionality   | High   | Medium     | Test each batch thoroughly before moving on                  |
| Over-restricting access           | Medium | High       | Document public endpoints clearly; test with different roles |
| Permission policy not registered  | High   | Low        | Verify all used policies exist in DependencyInjection.cs     |
| Inconsistent permission naming    | Medium | Medium     | Follow naming conventions; review existing policies first    |
| Student endpoints too restrictive | Medium | Medium     | Distinguish between student-accessible and admin-only        |

### Rollback Plan

- Each batch is independent; can rollback individual batch changes
- Keep track of modified files per batch
- Use git commits per batch for easy rollback
- Test in development environment before production

---

## 📅 Estimated Timeline

| Batch   | Duration  | Dependencies | Start After        |
| ------- | --------- | ------------ | ------------------ |
| Batch 1 | 4-6 hours | None         | Immediate          |
| Batch 2 | 4-6 hours | None         | Batch 1 testing    |
| Batch 3 | 6-8 hours | None         | Batch 2 testing    |
| Batch 4 | 3-4 hours | Batch 3      | Batch 3 complete   |
| Batch 5 | 4-5 hours | None         | Batch 2 testing    |
| Batch 6 | 4-5 hours | None         | Batch 5 testing    |
| Batch 7 | 3-4 hours | None         | Any batch complete |

**Total Estimated Time:** 28-38 hours  
**Recommended Schedule:** 1-2 batches per day over 2 weeks

---

## 🔄 Progress Tracking

### Batch Completion Status

- [ ] **Batch 1:** High Priority Admin Management
- [ ] **Batch 2:** Student Management
- [ ] **Batch 3:** Academy - Core Learning
- [ ] **Batch 4:** Academy - Student Progress
- [ ] **Batch 5:** Content Management - QA & Media
- [ ] **Batch 6:** Community Features
- [ ] **Batch 7:** System & Utilities

### Notes & Discoveries

_Document any issues, edge cases, or important decisions here as work progresses._

---

## 📞 Support & Questions

For questions or clarifications during implementation:

- Review PERMISSIONS_README.md for permission definitions
- Check existing Permissions.cs endpoint for reference implementation
- Consult Domain/Constants/Policies.cs for available policies
- Review Web/Infrastructure/DependencyInjection.cs for policy registration

---

**Last Updated:** February 6, 2026  
**Version:** 1.0  
**Author:** GitHub Copilot
