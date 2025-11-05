using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(ILogger<ApplicationDbContextInitialiser> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
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
        // --- Default Roles ---
        var roles = new[]
        {
            Roles.Administrator,
            Roles.Student,
            Roles.NonMuslim,
            Roles.Preacher,
            Roles.Instructor
        };

        foreach (var role in roles)
        {
            if (!_roleManager.Roles.Any(r => r.Name == role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // --- Default Users ---
        var users = new List<(string Email, string Password, string Role)>
        {
            ("administrator@localhost", "Administrator1!", Roles.Administrator),
            ("student@localhost", "Student1!", Roles.Student),
            ("nonmuslim@localhost", "NonMuslim1!", Roles.NonMuslim),
            ("preacher@localhost", "Preacher1!", Roles.Preacher),
            ("instructor@localhost", "Instructor1!", Roles.Instructor)
        };

        foreach (var (email, password, role) in users)
        {
            if (_userManager.Users.All(u => u.UserName != email))
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true // optional
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded && !string.IsNullOrWhiteSpace(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
                else if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create user {Email}: {Errors}", email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

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
                new Level { Title = "Beginner", Description = "Introductory level for new learners.", Difficulty = default },
                new Level { Title = "Intermediate", Description = "For learners with some background.", Difficulty = default },
                new Level { Title = "Advanced", Description = "In-depth topics and advanced study.", Difficulty = default }
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
                    Rating =4.7
                },
                new Course
                {
                    Title = "Quranic Recitation Basics",
                    Description = "Learn tajweed and basic recitation.",
                    Category = firstCategory,
                    Level = firstLevel,
                    IsPublished = true,
                    Rating =4.5
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
                StudentId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
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
                    EnrollmentId = enrollment.Id,
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
                Url = "https://example.com/certificates/sample.pdf",
                IssuedAt = DateTime.UtcNow
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
        }
    }
}
