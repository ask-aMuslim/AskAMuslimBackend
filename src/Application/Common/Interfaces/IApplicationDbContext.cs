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
    DbSet<StudentProgress> StudentProgresses { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<Question> Questions { get; }
    DbSet<Option> Options { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; }
    DbSet<Certificate> Certificates { get; }
    DbSet<StudentNote> StudentNotes { get; }
    DbSet<StudentQuestion> StudentQuestions { get; }
    DbSet<AskAMuslimBackend.Domain.Entities.LessonFeedback> LessonFeedbacks { get; }
    DbSet<QA> QAs { get; }
    DbSet<QATranslation> QATranslations { get; }
    DbSet<QATag> QATags { get; }
    DbSet<MeetingRequest> MeetingRequests { get; }
    DbSet<InquiryRequest> InquiryRequests { get; }
    DbSet<Event> Events { get; }
    DbSet<EventRegistration> EventRegistrations { get; }
    DbSet<Report> Reports { get; }
    DbSet<Achievement> Achievements { get; }
    DbSet<Mosque> Mosques { get; }

    // User Management
    DbSet<Student> Students { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<AppLanguage> AppLanguages { get; }
    DbSet<Instructor> Instructors { get; }
    DbSet<Admin> Admins { get; }
    DbSet<Preacher> Preachers { get; }
    DbSet<AdminNote> AdminNotes { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<UserPermission> UserPermissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<NewsletterSubscription> NewsletterSubscriptions { get; }

    // MuslimTube
    DbSet<MuslimTubeChannel> MuslimTubeChannels { get; }
    DbSet<MuslimTubeVideo> MuslimTubeVideos { get; }

    DbSet<Volunteer> Volunteers { get; }
    DbSet<LearnIslamRequest> LearnIslamRequests { get; }

    // Forms
    DbSet<Form> Forms { get; }
    DbSet<FormField> FormFields { get; }
    DbSet<FormFieldOption> FormFieldOptions { get; }
    DbSet<FormSubmission> FormSubmissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
