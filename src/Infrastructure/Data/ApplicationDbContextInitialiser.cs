using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // --- Default Users (no need for roles anymore, role is part of User entity) ---
        var users = new List<(string Email, string Password, string FirstName, string LastName, Domain.Enums.UserRole UserRole)>
        {
            ("administrator@localhost", "Administrator1!", "Admin", "User", Domain.Enums.UserRole.Admin),
            ("student@localhost", "Student1!", "John", "Student", Domain.Enums.UserRole.Student),
            ("nonmuslim@localhost", "NonMuslim1!", "Jane", "NonMuslim", Domain.Enums.UserRole.NonMuslim),
            ("instructor@localhost", "Instructor1!", "Bob", "Instructor", Domain.Enums.UserRole.Instructor)
        };

        foreach (var (email, password, firstName, lastName, userRole) in users)
        {
            if (!_context.Users.Any(u => u.Email == email))
            {
                var user = new User
                {
                    Email = email,
                    PasswordHash = _passwordHasher.HashPassword(password),
                    FirstName = firstName,
                    LastName = lastName,
                    Role = userRole,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                // Create role-specific entity
                await CreateRoleSpecificEntity(user.Id, userRole);
            }
        }

        // Obtain the Student entity ID (not User ID) for FK assignments
        var studentUserId = _context.Users.FirstOrDefault(u => u.Email == "student@localhost")?.Id;
        if (studentUserId == null)
        {
            _logger.LogWarning("Student user not found; skipping student-related seed data.");
            return; // Cannot seed dependent entities without a valid user
        }

        var studentEntity = _context.Students.FirstOrDefault(s => s.UserId == studentUserId.Value);
        if (studentEntity == null)
        {
            _logger.LogWarning("Student entity not found for user; skipping student-related seed data.");
            return;
        }

        var studentId = studentEntity.Id; // This is the Student entity's Id, not the User's Id

        // Default data
        // Seed, if necessary
        if (!_context.TodoLists.Any())
        {
            _context.TodoLists.Add(new TodoList
            {
                Title = "Todo List",
                Items =
                {
                    new TodoItem { Title = "Make a todo list 📃" },
                    new TodoItem { Title = "Check off the first item ✅" },
                    new TodoItem { Title = "Realise you've already done two things on the list! 🤯"},
                    new TodoItem { Title = "Reward yourself with a nice, long nap 🏆" },
                }
            });

            await _context.SaveChangesAsync();
        }

        // --- Categories ---

        if (!_context.Categories.Any())
        {
            var categories = new List<Category>
            {
                new Category { Title = "Islamic Studies", Description = "Foundational topics on Islam." },
                new Category { Title = "Quran", Description = "Quranic studies and recitation." },
                new Category { Title = "Hadith", Description = "Prophetic traditions and explanations." }
            };
            
            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        // --- Levels ---
        if (!_context.Levels.Any())
        {
            var levels = new List<Level>
            {
                new Level { Title = "Beginner", Description = "Introductory level for new learners.", Difficulty = default, Order = 1 },
                new Level { Title = "Intermediate", Description = "For learners with some background.", Difficulty = default, Order = 2 },
                new Level { Title = "Advanced", Description = "In-depth topics and advanced study.", Difficulty = default, Order = 3 }
            };

            _context.Levels.AddRange(levels);
            await _context.SaveChangesAsync();
        }

        // --- Courses ---
        if (!_context.Courses.Any())
        {
            var firstCategory = _context.Categories.First();
            var firstLevel = _context.Levels.First();

            var courses = new List<Course>
            {
                new Course
                {
                    Title = "Introduction to Islam",
                    Description = "Basics of belief, practice and history.",
                    Category = firstCategory,
                    Level = firstLevel,
                    IsPublished = true,
                    Rating =4.7,
                    Order = 1
                },
                new Course
                {
                    Title = "Quranic Recitation Basics",
                    Description = "Learn tajweed and basic recitation.",
                    Category = firstCategory,
                    Level = firstLevel,
                    IsPublished = true,
                    Rating =4.5,
                    Order = 2
                }
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();
        }

        // --- Lessons ---
        if (!_context.Lessons.Any())
        {
            var course = _context.Courses.First();

            var lessons = new List<Lesson>
            {
                new Lesson { Title = "What is Islam?", Description = "Overview of Islam.", Course = course, Order =1 },
                new Lesson { Title = "Five Pillars", Description = "Detailed look at the five pillars.", Course = course, Order =2 }
            };

            _context.Lessons.AddRange(lessons);
            await _context.SaveChangesAsync();
        }

        // --- Quizzes, Questions, Options ---
        if (!_context.Quizzes.Any())
        {
            var course = _context.Courses.First();

            var quiz = new Quiz
            {
                Title = "Introductory Quiz",
                TargetId = course.Id,
                TotalMarks = 10,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Text = "Which of the following is a pillar of Islam?",
                        Options = new List<Option>
                        {
                            new Option { Text = "Shahada", IsCorrect = true },
                            new Option { Text = "Reincarnation", IsCorrect = false },
                            new Option { Text = "Karma", IsCorrect = false }
                        }
                    }
                }
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
        }

        // --- Enrollments & QuizAttempts ---
        if (!_context.Enrollments.Any())
        {
            var course = _context.Courses.First();

            var enrollment = new Enrollment
            {
                StudentId = studentId, // use Student entity Id
                Course = course,
                EnrolledAt = DateTime.UtcNow,
                Status = default,
                CourseProgress = 0
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Add a quiz attempt for the enrollment if a quiz exists
            var quiz = _context.Quizzes.FirstOrDefault(q => q.TargetId == course.Id);
            if (quiz != null)
            {
                var attempt = new QuizAttempt
                {
                    QuizId = quiz.Id,
                    StudentId = studentId,
                    Score = 8.5
                };

                _context.QuizAttempts.Add(attempt);
                await _context.SaveChangesAsync();
            }
        }

        // --- Certificates ---
        if (!_context.Certificates.Any())
        {
            var level = _context.Levels.First();

            var certificate = new Certificate
            {
                Level = level,
                StudentId = studentId, // use Student entity Id
                Url = "https://example.com/certificates/sample.pdf",
                IssuedAt = DateTime.UtcNow
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
        }
    }

    private async Task CreateRoleSpecificEntity(Guid userId, Domain.Enums.UserRole role)
    {
        switch (role)
        {
            case Domain.Enums.UserRole.Student:
                if (!_context.Students.Any(s => s.UserId == userId))
                {
                    var student = new Student
                    {
                        UserId = userId,
                        EnrollmentDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                }
                break;

            case Domain.Enums.UserRole.Instructor:
                if (!_context.Instructors.Any(i => i.UserId == userId))
                {
                    var instructor = new Instructor
                    {
                        UserId = userId,
                        JoinedDate = DateTime.UtcNow,
                        IsActive = true,
                        IsVerified = false
                    };
                    _context.Instructors.Add(instructor);
                    await _context.SaveChangesAsync();
                }
                break;

            case Domain.Enums.UserRole.Admin:
                if (!_context.Admins.Any(a => a.UserId == userId))
                {
                    var admin = new Admin
                    {
                        UserId = userId,
                        JoinedDate = DateTime.UtcNow,
                        IsActive = true,
                        IsSuperAdmin = false,
                        CanManageUsers = true,
                        CanManageCourses = true,
                        CanManageContent = true,
                        CanViewReports = true,
                        CanManageRoles = false
                    };
                    _context.Admins.Add(admin);
                    await _context.SaveChangesAsync();
                }
                break;

            case Domain.Enums.UserRole.NonMuslim:
                // NonMuslim users are treated as students
                if (!_context.Students.Any(s => s.UserId == userId))
                {
                    var nonMuslimStudent = new Student
                    {
                        UserId = userId,
                        EnrollmentDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Students.Add(nonMuslimStudent);
                    await _context.SaveChangesAsync();
                }
                break;
        }
    }
}
