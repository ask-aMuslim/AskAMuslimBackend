using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    private readonly IHostEnvironment _env;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IHostEnvironment env)
    {
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
        _env = env;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_env.IsDevelopment())
            {
                await _context.Database.EnsureDeletedAsync();
                await _context.Database.EnsureCreatedAsync();
            }
            else
            {
                await _context.Database.MigrateAsync();
            }
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
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // --- Default Users (no need for roles anymore, role is part of User entity) ---
        var userSeeds = new List<(string Email, string Password, string FirstName, string LastName, UserRole UserRole)>
        {
            ("administrator@localhost", "Administrator1!", "Admin", "User", UserRole.Admin),
            ("student@localhost", "Student1!", "John", "Student", UserRole.Student),
            ("nonmuslim@localhost", "NonMuslim1!", "Jane", "NonMuslim", UserRole.NonMuslim),
            ("instructor@localhost", "Instructor1!", "Bob", "Instructor", UserRole.Instructor)
        };

        var seedEmails = userSeeds.Select(s => s.Email).ToList();
        var existingEmails = await _context.Users
            .AsNoTracking()
            .Where(u => seedEmails.Contains(u.Email))
            .Select(u => u.Email)
            .ToListAsync();

        var newUsers = new List<User>();
        foreach (var (email, password, firstName, lastName, userRole) in userSeeds)
        {
            if (existingEmails.Contains(email)) continue;

            newUsers.Add(new User
            {
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(password),
                FirstName = firstName,
                LastName = lastName,
                Role = userRole,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newUsers.Count > 0)
        {
            _context.Users.AddRange(newUsers);
            await _context.SaveChangesAsync();

            // Create role-specific entities without saving each time; save once after.
            foreach (var user in newUsers)
            {
                await CreateRoleSpecificEntity(user.Id, user.Role);
            }
            await _context.SaveChangesAsync();
        }

        // Obtain the Student entity ID (not User ID) for FK assignments
        var studentUserId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Email == "student@localhost")
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync();

        if (studentUserId is null)
        {
            _logger.LogWarning("Student user not found; skipping student-related seed data.");
            await transaction.CommitAsync();
            return; // Cannot seed dependent entities without a valid user
        }

        var studentId = await _context.Students
            .AsNoTracking()
            .Where(s => s.UserId == studentUserId.Value)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync();

        if (studentId is null)
        {
            _logger.LogWarning("Student entity not found for user; skipping student-related seed data.");
            await transaction.CommitAsync();
            return;
        }

        var studentEntityId = studentId.Value; // This is the Student entity's Id, not the User's Id

        // Default data
        // Seed, if necessary
        if (!await _context.TodoLists.AsNoTracking().AnyAsync())
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

        if (!await _context.Categories.AsNoTracking().AnyAsync())
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
        if (!await _context.Levels.AsNoTracking().AnyAsync())
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
        if (!await _context.Courses.AsNoTracking().AnyAsync())
        {
            var firstCategory = await _context.Categories.AsNoTracking().OrderBy(c => c.Title).FirstAsync();
            var firstLevel = await _context.Levels.AsNoTracking().OrderBy(l => l.Order).FirstAsync();

            var courses = new List<Course>
            {
                new Course
                {
                    Title = "Introduction to Islam",
                    Description = "Basics of belief, practice and history.",
                    CategoryId = firstCategory.Id,
                    LevelId = firstLevel.Id,
                    IsPublished = true,
                    Rating = 4.7,
                    Order = 1
                },
                new Course
                {
                    Title = "Quranic Recitation Basics",
                    Description = "Learn tajweed and basic recitation.",
                    CategoryId = firstCategory.Id,
                    LevelId = firstLevel.Id,
                    IsPublished = true,
                    Rating = 4.5,
                    Order = 2
                }
            };

            _context.Courses.AddRange(courses);
            await _context.SaveChangesAsync();
        }

        // --- Lessons ---
        if (!await _context.Lessons.AsNoTracking().AnyAsync())
        {
            var course = await _context.Courses.AsNoTracking().OrderBy(c => c.Order).FirstAsync();

            var lessons = new List<Lesson>
            {
                new Lesson { Title = "What is Islam?", Description = "Overview of Islam.", CourseId = course.Id, Order = 1 },
                new Lesson { Title = "Five Pillars", Description = "Detailed look at the five pillars.", CourseId = course.Id, Order = 2 }
            };

            _context.Lessons.AddRange(lessons);
            await _context.SaveChangesAsync();
        }

        // --- Quizzes, Questions, Options ---
        if (!await _context.Quizzes.AsNoTracking().AnyAsync())
        {
            var course = await _context.Courses.AsNoTracking().OrderBy(c => c.Order).FirstAsync(); // deterministic

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
        if (!await _context.Enrollments.AsNoTracking().AnyAsync())
        {
            var course = await _context.Courses.AsNoTracking().OrderBy(c => c.Order).FirstAsync(); // deterministic

            var enrollment = new Enrollment
            {
                StudentId = studentEntityId, // use Student entity Id
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Active, // avoid undefined enum value
                LevelProgress = 0,
                CourseProgress = 0
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Add a quiz attempt for the enrollment if a quiz exists
            var quiz = await _context.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.TargetId == course.Id);
            if (quiz != null)
            {
                var attempt = new QuizAttempt
                {
                    QuizId = quiz.Id,
                    StudentId = studentEntityId,
                    Score = 8.5M
                };

                _context.QuizAttempts.Add(attempt);
                await _context.SaveChangesAsync();
            }
        }

        // --- Certificates ---
        if (!await _context.Certificates.AsNoTracking().AnyAsync())
        {
            var level = await _context.Levels.AsNoTracking().OrderBy(l => l.Order).FirstAsync(); // deterministic

            var certificate = new Certificate
            {
                LevelId = level.Id,
                StudentId = studentEntityId, // use Student entity Id
                Url = "https://example.com/certificates/sample.pdf",
                IssuedAt = DateTime.UtcNow
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
    }

    private Task CreateRoleSpecificEntity(Guid userId, UserRole role)
    {
        switch (role)
        {
            case UserRole.Student:
                if (!_context.Students.AsNoTracking().Any(s => s.UserId == userId))
                {
                    _context.Students.Add(new Student
                    {
                        UserId = userId,
                        EnrollmentDate = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                break;

            case UserRole.Instructor:
                if (!_context.Instructors.AsNoTracking().Any(i => i.UserId == userId))
                {
                    _context.Instructors.Add(new Instructor
                    {
                        UserId = userId,
                        JoinedDate = DateTime.UtcNow,
                        IsActive = true,
                        IsVerified = false
                    });
                }
                break;

            case UserRole.Admin:
                if (!_context.Admins.AsNoTracking().Any(a => a.UserId == userId))
                {
                    _context.Admins.Add(new Admin
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
                    });
                }
                break;

            case UserRole.NonMuslim:
                // NonMuslim users are treated as students
                if (!_context.Students.AsNoTracking().Any(s => s.UserId == userId))
                {
                    _context.Students.Add(new Student
                    {
                        UserId = userId,
                        EnrollmentDate = DateTime.UtcNow,
                        IsActive = true
                    });
                }
                break;
        }

        return Task.CompletedTask;
    }
}
