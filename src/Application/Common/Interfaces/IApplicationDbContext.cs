using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoList> TodoLists { get; }
    DbSet<TodoItem> TodoItems { get; }

    // Content
    DbSet<Tag> Tags { get; }
    DbSet<CourseTag> CourseTags { get; }
    DbSet<Level> Levels { get; }
    DbSet<Course> Courses { get; }
    DbSet<CoursePrerequisite> CoursePrerequisites { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<Question> Questions { get; }
    DbSet<Option> Options { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; }
    DbSet<Certificate> Certificates { get; }
    DbSet<StudentNote> StudentNotes { get; }
    DbSet<StudentQuestion> StudentQuestions { get; }
    DbSet<QA> QAs { get; }
    DbSet<QATranslation> QATranslations { get; }
    DbSet<QATag> QATags { get; }
    DbSet<MeetingRequest> MeetingRequests { get; }
    DbSet<InquiryRequest> InquiryRequests { get; }
    DbSet<Event> Events { get; }
    DbSet<EventRegistration> EventRegistrations { get; }
    DbSet<Report> Reports { get; }

    // User Management
    DbSet<Student> Students { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<Instructor> Instructors { get; }
    DbSet<Admin> Admins { get; }
    DbSet<Preacher> Preachers { get; }
    DbSet<AdminNote> AdminNotes { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<UserPermission> UserPermissions { get; }
    DbSet<RolePermission> RolePermissions { get; }

    // MuslimTube
    DbSet<MuslimTubeChannel> MuslimTubeChannels { get; }
    DbSet<MuslimTubeVideo> MuslimTubeVideos { get; }

    DbSet<Volunteer> Volunteers { get; }
    DbSet<LearnIslamRequest> LearnIslamRequests { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
