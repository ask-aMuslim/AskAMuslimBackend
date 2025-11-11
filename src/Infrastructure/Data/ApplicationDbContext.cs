using System.Reflection;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CourseTag> CourseTags => Set<CourseTag>();
    public DbSet<Level> Levels => Set<Level>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<StudentNote> StudentNotes => Set<StudentNote>();
    public DbSet<StudentQuestion> StudentQuestions => Set<StudentQuestion>();
    public DbSet<QA> QAs => Set<QA>();
    public DbSet<QATranslation> QATranslations => Set<QATranslation>();
    public DbSet<QATag> QATags => Set<QATag>();
    
    // User Management
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Admin> Admins => Set<Admin>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
