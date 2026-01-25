using System.Globalization;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _configuration;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IHostEnvironment env,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _env = env;
        _configuration = configuration;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            // Check if database can be connected
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("Cannot connect to database. Skipping initialization.");
                return;
            }

            // Use migrations for all environments since we don't have CREATE DATABASE permissions
            await _context.Database.MigrateAsync();
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
            // Check if database can be connected before seeding
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("Cannot connect to database. Skipping seeding.");
                return;
            }

            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Seeding skipped due to an error. Proceeding without seed to avoid build-time failures.");
            // Do not rethrow here to avoid failing build tooling (e.g., NSwag) which executes the app during generation.
            // In a normal run, seeding issues should be investigated and resolved.
        }
    }

    public async Task TrySeedAsync()
    {
        // Use execution strategy to wrap transaction for retry compatibility
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // --- Seed Identity Roles ---
            var roles = new[] { "Admin", "Student", "Instructor", "NonMuslim", "Preacher" };
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }

            // --- Default Users ---
            // Read admin configuration from appsettings
            var adminEmail = _configuration["AdminSeed:Email"] ?? "administrator@localhost";
            var adminPassword = _configuration["AdminSeed:Password"] ?? "Administrator1!";
            var adminFirstName = _configuration["AdminSeed:FirstName"] ?? "Admin";
            var adminLastName = _configuration["AdminSeed:LastName"] ?? "User";

            var userSeeds = new List<(string Email, string Password, string FirstName, string LastName, UserRole UserRole, ReligiousStatus religion)>
        {
            (adminEmail, adminPassword, adminFirstName, adminLastName, UserRole.Admin, ReligiousStatus.BornMuslim),
            ("student@localhost", "Student1!", "John", "Student", UserRole.Student, ReligiousStatus.RevertedMuslim),
            ("student2@localhost", "Student2!", "Sara", "Learner", UserRole.Student, ReligiousStatus.RevertedMuslim),
            ("nonmuslim@localhost", "NonMuslim1!", "Jane", "NonMuslim", UserRole.NonMuslim, ReligiousStatus.NonMuslim),
            ("nonmuslim2@localhost", "NonMuslim2!", "Liam", "Guest", UserRole.NonMuslim, ReligiousStatus.NonMuslim),
            ("instructor@localhost", "Instructor1!", "Bob", "Instructor", UserRole.Instructor, ReligiousStatus.BornMuslim),
            ("instructor2@localhost", "Instructor2!", "Fatima", "Khan", UserRole.Instructor, ReligiousStatus.BornMuslim),
            ("instructor3@localhost", "Instructor3!", "Omar", "Saeed", UserRole.Instructor, ReligiousStatus.BornMuslim),
            ("preacher@localhost", "Preacher1!", "Ahmed", "Preacher", UserRole.Preacher, ReligiousStatus.BornMuslim),
            ("preacher2@localhost", "Preacher2!", "Ibrahim", "Saleh", UserRole.Preacher, ReligiousStatus.BornMuslim),
            ("admin2@localhost", "Admin2!", "Alice", "Adminson", UserRole.Admin, ReligiousStatus.BornMuslim)
        };

            var newUsers = new List<User>();
            foreach (var (email, password, firstName, lastName, userRole, religousStatus) in userSeeds)
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null) continue;

                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = userRole,
                    ReligionStatus = religousStatus,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // ReligionStatus is just a flag here
                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, userRole.ToString());
                    newUsers.Add(user);
                }
                else
                {
                    _logger.LogWarning("Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            if (newUsers.Count > 0)
            {
                // Create role-specific entities without saving each time; save once after.
                foreach (var user in newUsers)
                {
                    await CreateRoleSpecificEntity(user.Id, user.Role);
                }
                await _context.SaveChangesAsync();
            }

            // Ensure users have required basic fields (Email, FirstName, LastName)
            var usersWithMissing = await _context.Users
                .Where(u => string.IsNullOrWhiteSpace(u.Email) || string.IsNullOrWhiteSpace(u.FirstName) || string.IsNullOrWhiteSpace(u.LastName))
                .ToListAsync();

            foreach (var u in usersWithMissing)
            {
                var changed = false;

                if (string.IsNullOrWhiteSpace(u.Email))
                {
                    u.Email = $"{u.Id}@localhost";
                    u.UserName = u.Email;
                    u.NormalizedEmail = u.Email.ToUpperInvariant();
                    u.NormalizedUserName = u.UserName?.ToUpperInvariant();
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(u.FirstName))
                {
                    var local = (u.Email ?? u.Id.ToString()).Split('@')[0];
                    var parts = local.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    u.FirstName = parts.Length > 0 ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0]) : "User";
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(u.LastName))
                {
                    var local = (u.Email ?? u.Id.ToString()).Split('@')[0];
                    var parts = local.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    u.LastName = parts.Length > 1 ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[1]) : "User";
                    changed = true;
                }

                if (changed)
                {
                    _context.Users.Update(u);
                }
            }

            if (usersWithMissing.Count > 0)
            {
                await _context.SaveChangesAsync();
            }

            // Ensure users referenced by role-specific entities also have basic fields
            var referencedUserIds = new List<Guid>();

            referencedUserIds.AddRange(await _context.Preachers.AsNoTracking().Select(p => p.UserId).ToListAsync());
            referencedUserIds.AddRange(await _context.Instructors.AsNoTracking().Select(i => i.UserId).ToListAsync());
            referencedUserIds.AddRange(await _context.Admins.AsNoTracking().Select(a => a.UserId).ToListAsync());
            referencedUserIds.AddRange(await _context.Students.AsNoTracking().Select(s => s.UserId).ToListAsync());

            referencedUserIds = referencedUserIds.Distinct().ToList();

            var changedAny = false;
            foreach (var uid in referencedUserIds)
            {
                var u = await _context.Users.FindAsync(new object[] { uid });
                if (u == null) continue;

                var changed = false;
                if (string.IsNullOrWhiteSpace(u.Email))
                {
                    u.Email = $"{u.Id}@localhost";
                    u.UserName = u.Email;
                    u.NormalizedEmail = u.Email.ToUpperInvariant();
                    u.NormalizedUserName = u.UserName?.ToUpperInvariant();
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(u.FirstName) || string.IsNullOrWhiteSpace(u.LastName))
                {
                    // derive from email local part or use role-based defaults
                    var local = (u.Email ?? u.Id.ToString()).Split('@')[0];
                    var parts = local.Split(new[] { '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    if (string.IsNullOrWhiteSpace(u.FirstName))
                    {
                        u.FirstName = parts.Length > 0 ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[0]) : "User";
                        changed = true;
                    }

                    if (string.IsNullOrWhiteSpace(u.LastName))
                    {
                        u.LastName = parts.Length > 1 ? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(parts[1]) : "User";
                        changed = true;
                    }
                }

                if (changed)
                {
                    _context.Users.Update(u);
                    changedAny = true;
                }
            }

            if (changedAny)
            {
                await _context.SaveChangesAsync();
            }

            // Obtain Student entity Id for FK seeds
            var studentUser = await _userManager.FindByEmailAsync("student@localhost");
            if (studentUser is null)
            {
                _logger.LogWarning("Student user not found; skipping student-related seed data.");
                await transaction.CommitAsync();
                return;
            }

            var studentId = await _context.Students
                .AsNoTracking()
                .Where(s => s.UserId == studentUser.Id)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync();

            if (studentId is null)
            {
                _logger.LogWarning("Student entity not found for user; skipping student-related seed data.");
                await transaction.CommitAsync();
                return;
            }

            var studentEntityId = studentId.Value;

            // --- Todo List ---
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

            // --- Tags ---
            if (!await _context.Tags.AsNoTracking().AnyAsync())
            {
                var tags = new List<Tag>
            {
                new Tag { Title = "Islamic Studies", Description = "Foundational topics on Islam." },
                new Tag { Title = "Quran", Description = "Quranic studies and recitation." },
                new Tag { Title = "Hadith", Description = "Prophetic traditions and explanations." },
                new Tag { Title = "Beginner Friendly", Description = "Suitable for newcomers." }
            };
                _context.Tags.AddRange(tags);
                await _context.SaveChangesAsync();
            }

            // --- Levels ---
            if (!await _context.Levels.AsNoTracking().AnyAsync())
            {
                var levels = new List<Level>
            {
                new Level { Title = "Beginner", Description = "Introductory level for new learners.", Difficulty = LevelDifficulty.Beginner, Order = 1 },
                new Level { Title = "Intermediate", Description = "For learners with some background.", Difficulty = LevelDifficulty.Intermediate, Order = 2 },
                new Level { Title = "Advanced", Description = "In-depth topics and advanced study.", Difficulty = LevelDifficulty.Advanced, Order = 3 }
            };

                _context.Levels.AddRange(levels);
                await _context.SaveChangesAsync();
            }

            // --- Courses & CourseTags ---
            if (!await _context.Courses.AsNoTracking().AnyAsync())
            {
                var firstLevel = await _context.Levels.AsNoTracking().OrderBy(l => l.Order).FirstAsync();
                var tagIds = await _context.Tags.AsNoTracking().OrderBy(t => t.Title).Select(t => t.Id).ToListAsync();

                var introCourse = new Course
                {
                    Title = "Introduction to Islam",
                    Description = "Basics of belief, practice and history.",
                    LevelId = firstLevel.Id,
                    IsPublished = true,
                    Rating = 4.7,
                    Order = 1
                };

                var recitationCourse = new Course
                {
                    Title = "Quranic Recitation Basics",
                    Description = "Learn tajweed and basic recitation.",
                    LevelId = firstLevel.Id,
                    IsPublished = true,
                    Rating = 4.5,
                    Order = 2
                };

                _context.Courses.AddRange(introCourse, recitationCourse);
                await _context.SaveChangesAsync();

                // Link some tags (first two tags to first course, first + third to second)
                if (tagIds.Count >= 3)
                {
                    var courseTags = new List<CourseTag>
                {
                    new CourseTag { CourseId = introCourse.Id, TagId = tagIds[0] },
                    new CourseTag { CourseId = introCourse.Id, TagId = tagIds[1] },
                    new CourseTag { CourseId = recitationCourse.Id, TagId = tagIds[0] },
                    new CourseTag { CourseId = recitationCourse.Id, TagId = tagIds[2] }
                };
                    _context.CourseTags.AddRange(courseTags);
                    await _context.SaveChangesAsync();
                }
            }

            // --- Lessons ---
            if (!await _context.Lessons.AsNoTracking().AnyAsync())
            {
                var course = await _context.Courses.AsNoTracking().OrderBy(c => c.Order).FirstAsync();

                var lessons = new List<Lesson>
                {
                    new Lesson { Title = "What is Islam?", Description = "Overview of Islam.", CourseId = course.Id, Order = 1, Type = LessonType.Article, IsPublished = true,
                        ContentJson = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Islam is a monotheistic faith centered on submission to Allah and following the teachings of the Prophet Muhammad (peace be upon him).\"}]}]}" },

                    new Lesson { Title = "Five Pillars", Description = "Detailed look at the five pillars.", CourseId = course.Id, Order = 2, Type = LessonType.Article, IsPublished = true,
                        ContentJson = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"The Five Pillars of Islam are: Shahada, Salah, Zakat, Sawm, and Hajj.\"}]}]}" },

                    // Video lesson example
                    new Lesson { Title = "Tajweed Basics (Video)", Description = "Learn the basics of Quranic recitation and tajweed rules.", CourseId = course.Id, Order = 3, Type = LessonType.Video, IsPublished = true,
                        ContentUrl = "https://example.com/media/videos/tajweed-basics.mp4",
                        ThumbnailUrl = "https://example.com/media/thumbnails/tajweed-basics.jpg",
                        Transcript = "This video covers the basic tajweed rules including ikhfa, idgham and qalqalah. Follow along with the recitation examples." },

                    // Audio lesson example
                    new Lesson { Title = "Recitation Practice (Audio)", Description = "Short audio practice for beginners.", CourseId = course.Id, Order = 4, Type = LessonType.Audio, IsPublished = true,
                        ContentUrl = "https://example.com/media/audio/recitation-practice.mp3",
                        Transcript = "Audio practice track with pauses for repetition. Listen and repeat after the reciter.",
                        ThumbnailUrl = "https://example.com/media/thumbnails/recitation-audio.jpg" },
                };

                _context.Lessons.AddRange(lessons);
                await _context.SaveChangesAsync();
            }

            // --- Quizzes, Questions, Options ---
            if (!await _context.Quizzes.AsNoTracking().AnyAsync())
            {
                var course = await _context.Courses.AsNoTracking().OrderBy(c => c.Order).FirstAsync();

                // Course quiz
                var quiz = new Quiz
                {
                    Title = "Introductory Quiz",
                    CourseId = course.Id,
                    TargetType = QuizTargetType.Course,
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

                // Level quiz
                var levelId = course.LevelId;
                _context.Quizzes.Add(new Quiz
                {
                    Title = "Level Check",
                    LevelId = levelId,
                    TargetType = QuizTargetType.Level,
                    TotalMarks = 10
                });

                // Lesson quiz (for the first lesson)
                var firstLesson = await _context.Lessons.AsNoTracking().Where(l => l.CourseId == course.Id).OrderBy(l => l.Order).FirstOrDefaultAsync();
                if (firstLesson != null)
                {
                    _context.Quizzes.Add(new Quiz
                    {
                        Title = "Lesson Quick Quiz",
                        LessonId = firstLesson.Id,
                        TargetType = QuizTargetType.Lesson,
                        TotalMarks = 10
                    });
                }

                // Lesson quiz (for the second lesson)
                var secondLesson = await _context.Lessons.AsNoTracking().Where(l => l.CourseId == course.Id).OrderBy(l => l.Order).Skip(1).FirstOrDefaultAsync();
                if (secondLesson != null)
                {
                    _context.Quizzes.Add(new Quiz
                    {
                        Title = "Lesson Quick Quiz 2",
                        LessonId = secondLesson.Id,
                        TargetType = QuizTargetType.Lesson,
                        TotalMarks = 10
                    });
                }

                await _context.SaveChangesAsync();
            }

            // --- Enrollments & QuizAttempts ---
            if (!await _context.Enrollments.AsNoTracking().AnyAsync())
            {
                var course = await _context.Courses.AsNoTracking().OrderBy(c => c.Order).FirstAsync();

                var enrollment = new Enrollment
                {
                    StudentId = studentEntityId,
                    CourseId = course.Id,
                    EnrolledAt = DateTime.UtcNow,
                    Status = EnrollmentStatus.Active,
                    LevelProgress = 0,
                    CourseProgress = 0
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                var quiz = await _context.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.CourseId == course.Id);
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

            // --- Student Notes ---
            if (!await _context.StudentNotes.AsNoTracking().AnyAsync())
            {
                var firstLesson = await _context.Lessons.AsNoTracking().OrderBy(l => l.Order).FirstAsync();

                _context.StudentNotes.AddRange(
                    new StudentNote { StudentId = studentEntityId, LessonId = firstLesson.Id, Text = "Key point about Shahada.", Timestamp = 12.5 },
                    new StudentNote { StudentId = studentEntityId, LessonId = firstLesson.Id, Text = "Clarify Five Pillars order.", Timestamp = 89.3 }
                );
                await _context.SaveChangesAsync();
            }

            // --- Student Questions ---
            if (!await _context.StudentQuestions.AsNoTracking().AnyAsync())
            {
                var firstLesson = await _context.Lessons.AsNoTracking().OrderBy(l => l.Order).FirstAsync();

                _context.StudentQuestions.AddRange(
                    new StudentQuestion { StudentId = studentEntityId, LessonId = firstLesson.Id, QuestionText = "Can you expand on the meaning of Shahada?", Timestamp = 30, Answered = false },
                    new StudentQuestion { StudentId = studentEntityId, LessonId = firstLesson.Id, QuestionText = "Is Zakat mandatory for everyone?", Timestamp = 145, Answered = true, AnswerText = "Zakat is mandatory once nisab is reached." }
                );
                await _context.SaveChangesAsync();
            }

            // --- Certificates ---
            if (!await _context.Certificates.AsNoTracking().AnyAsync())
            {
                var level = await _context.Levels.AsNoTracking().OrderBy(l => l.Order).FirstAsync();

                var certificate = new Certificate
                {
                    LevelId = level.Id,
                    StudentId = studentEntityId,
                    Url = "https://example.com/certificates/sample.pdf",
                    IssuedAt = DateTime.UtcNow
                };

                _context.Certificates.Add(certificate);
                await _context.SaveChangesAsync();
            }

            // --- QAs & QATranslations & QATags ---
            if (!await _context.QAs.AsNoTracking().AnyAsync())
            {
                var tagsByTitle = await _context.Tags.AsNoTracking().ToDictionaryAsync(t => t.Title, t => t.Id);

                var qa1 = new QA
                {
                    ImageUrl = null,
                    VideoUrl = null,
                    IsPublished = true,
                    Translations = new List<QATranslation>
                {
                    new QATranslation
                    {
                        Language = Language.English,
                        QuestionText = "What is Islam?",
                        AnswerText = "Islam is a monotheistic faith centered on submission to Allah and following the teachings of the Prophet Muhammad (peace be upon him).",
                        AnswerTextJson = @"{""type"":""doc"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""Islam is a ""},{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""monotheistic faith""},{""type"":""text"",""text"":"" centered on submission to Allah and following the teachings of the Prophet Muhammad (peace be upon him).""}]},{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""The Five Pillars of Islam are:""}]},{""type"":""bulletList"",""content"":[{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Shahada""},{""type"":""text"",""text"":"" - Declaration of Faith""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Salah""},{""type"":""text"",""text"":"" - Prayer (five times daily)""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Zakat""},{""type"":""text"",""text"":"" - Charity""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Sawm""},{""type"":""text"",""text"":"" - Fasting during Ramadan""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Hajj""},{""type"":""text"",""text"":"" - Pilgrimage to Mecca""}]}]}]}]}"
                    },
                    new QATranslation
                    {
                        Language = Language.Arabic,
                        QuestionText = "ما هو الإسلام؟",
                        AnswerText = "الإسلام هو دين توحيدي يقوم على عبادة الله وحده واتباع تعاليم النبي محمد ﷺ.",
                        AnswerTextJson = @"{""type"":""doc"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""الإسلام هو دين ""},{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""توحيدي""},{""type"":""text"",""text"":"" يقوم على عبادة الله وحده واتباع تعاليم النبي محمد ﷺ.""}]}]}"
                    }
                }
                };

                var qa2 = new QA
                {
                    ImageUrl = null,
                    VideoUrl = null,
                    IsPublished = true,
                    Translations = new List<QATranslation>
                {
                    new QATranslation
                    {
                        Language = Language.English,
                        QuestionText = "Who is Allah?",
                        AnswerText = "Allah is the Arabic word for God, the One and Only, the Creator of the heavens and the earth.",
                        AnswerTextJson = @"{""type"":""doc"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Allah""},{""type"":""text"",""text"":"" is the Arabic word for God, the ""},{""type"":""text"",""marks"":[{""type"":""italic""}],""text"":""One and Only""},{""type"":""text"",""text"":"", the Creator of the heavens and the earth.""}]},{""type"":""blockquote"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""italic""}],""text"":""Say, He is Allah, the One. Allah, the Eternal Refuge. He neither begets nor is born, nor is there to Him any equivalent.""}]}]},{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""(Quran 112:1-4)""}]}]}"
                    },
                    new QATranslation
                    {
                        Language = Language.Arabic,
                        QuestionText = "من هو الله؟",
                        AnswerText = "الله هو اسم الجلالة، الإله الواحد الأحد خالق السماوات والأرض.",
                        AnswerTextJson = @"{""type"":""doc"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""الله هو اسم الجلالة، الإله ""},{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""الواحد الأحد""},{""type"":""text"",""text"":"" خالق السماوات والأرض.""}]}]}"
                    }
                }
                };

                // Additional QA: Prayer basics
                var qa3 = new QA
                {
                    ImageUrl = null,
                    VideoUrl = null,
                    IsPublished = true,
                    Translations = new List<QATranslation>
                {
                    new QATranslation
                    {
                        Language = Language.English,
                        QuestionText = "How many daily prayers are there?",
                        AnswerText = "There are five obligatory prayers each day in Islam: Fajr, Dhuhr, Asr, Maghrib and Isha.",
                        AnswerTextJson = @"{""type"":""doc"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""There are ""},{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""five obligatory prayers""},{""type"":""text"",""text"":"" each day in Islam:""}]},{""type"":""orderedList"",""content"":[{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Fajr""},{""type"":""text"",""text"":"" - Dawn prayer (before sunrise)""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Dhuhr""},{""type"":""text"",""text"":"" - Noon prayer (after the sun passes its zenith)""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Asr""},{""type"":""text"",""text"":"" - Afternoon prayer""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Maghrib""},{""type"":""text"",""text"":"" - Sunset prayer""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""Isha""},{""type"":""text"",""text"":"" - Night prayer""}]}]}]},{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""These prayers are performed at specific times throughout the day and night.""}]}]}"
                    },
                    new QATranslation
                    {
                        Language = Language.Arabic,
                        QuestionText = "كم عدد الصلوات اليومية؟",
                        AnswerText = "هناك خمس صلوات مفروضة يومياً في الإسلام: الفجر، الظهر، العصر، المغرب، والعشاء.",
                        AnswerTextJson = @"{""type"":""doc"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""text"":""هناك ""},{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""خمس صلوات مفروضة""},{""type"":""text"",""text"":"" يومياً في الإسلام:""}]},{""type"":""orderedList"",""content"":[{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""الفجر""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""الظهر""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""العصر""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""المغرب""}]}]},{""type"":""listItem"",""content"":[{""type"":""paragraph"",""content"":[{""type"":""text"",""marks"":[{""type"":""bold""}],""text"":""العشاء""}]}]}]}]}"
                    }
                }
                };

                // Attach tags by title when available
                if (tagsByTitle.TryGetValue("Islamic Studies", out var islamicStudiesTag))
                {
                    qa1.QATags.Add(new QATag { TagId = islamicStudiesTag });
                    qa2.QATags.Add(new QATag { TagId = islamicStudiesTag });
                    qa3.QATags.Add(new QATag { TagId = islamicStudiesTag });
                }
                if (tagsByTitle.TryGetValue("Beginner Friendly", out var beginnerTag))
                {
                    qa1.QATags.Add(new QATag { TagId = beginnerTag });
                    qa3.QATags.Add(new QATag { TagId = beginnerTag });
                }

                _context.QAs.AddRange(qa1, qa2, qa3);
                await _context.SaveChangesAsync();
            }

            // --- Meeting Requests Seed ---
            if (!await _context.MeetingRequests.AsNoTracking().AnyAsync())
            {
                var requesterUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "student@localhost");
                var scholarUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "instructor@localhost");
                if (requesterUser != null && scholarUser != null)
                {
                    _context.MeetingRequests.Add(new MeetingRequest
                    {
                        RequesterId = requesterUser.Id,
                        // No ScholarId until accepted
                        Topic = MeetingInquiryTopic.GeneralInquiry,
                        Message = "I would like to learn more about the basics of Islam.",
                        Languages = new List<Language> { Language.English },
                        ScheduledAt = DateTime.UtcNow.AddDays(2),
                        Status = MeetingInquiryRequestStatus.Pending,
                    });
                    _context.MeetingRequests.Add(new MeetingRequest
                    {
                        RequesterId = requesterUser.Id,
                        ScholarId = scholarUser.Id,
                        Topic = MeetingInquiryTopic.Quran,
                        Message = "Questions about Quranic recitation rules.",
                        Languages = new List<Language> { Language.Arabic, Language.English },
                        ScheduledAt = DateTime.UtcNow.AddDays(5),
                        Status = MeetingInquiryRequestStatus.Accepted,
                        Response = "Looking forward to our meeting! here is the link https://meet.google.com/qwt-mxag-kyf"
                    });

                    // Additional meeting request with another time and mixed languages
                    _context.MeetingRequests.Add(new MeetingRequest
                    {
                        RequesterId = requesterUser.Id,
                        ScholarId = scholarUser.Id,
                        Topic = MeetingInquiryTopic.Fiqh,
                        Message = "Clarify fasting rules during travel.",
                        Languages = new List<Language> { Language.English },
                        ScheduledAt = DateTime.UtcNow.AddDays(10).AddHours(3),
                        Status = MeetingInquiryRequestStatus.Pending
                    });

                    await _context.SaveChangesAsync();
                }
            }

            // --- Inquiry Requests Seed ---
            if (!await _context.InquiryRequests.AsNoTracking().AnyAsync())
            {
                var requesterUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "student@localhost");
                var scholarUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "instructor@localhost");
                if (requesterUser != null && scholarUser != null)
                {
                    _context.InquiryRequests.Add(new InquiryRequest
                    {
                        RequesterId = requesterUser.Id,
                        // No ScholarId until accepted
                        Topic = MeetingInquiryTopic.GeneralInquiry,
                        Message = "I would like to learn more about the basics of Islam.",
                        Languages = new List<Language> { Language.English, Language.Arabic },
                        Status = MeetingInquiryRequestStatus.Pending,
                    });
                    _context.InquiryRequests.Add(new InquiryRequest
                    {
                        RequesterId = requesterUser.Id,
                        ScholarId = scholarUser.Id,
                        Topic = MeetingInquiryTopic.Quran,
                        Message = "Questions about Quranic recitation rules.",
                        Languages = new List<Language> { Language.Arabic },
                        Status = MeetingInquiryRequestStatus.Accepted,
                        Response = "Looking forward to our discussion!"
                    });

                    // Additional short inquiry
                    _context.InquiryRequests.Add(new InquiryRequest
                    {
                        RequesterId = requesterUser.Id,
                        Topic = MeetingInquiryTopic.Hadith,
                        Message = "Can you recommend beginner hadith collections?",
                        Languages = new List<Language> { Language.English },
                        Status = MeetingInquiryRequestStatus.Pending
                    });

                    await _context.SaveChangesAsync();
                }
            }

            // --- Events & EventRegistrations ---
            if (!await _context.Events.AsNoTracking().AnyAsync())
            {
                var speaker = "Dr. Ahmed";
                var now = DateTime.UtcNow;
                var event1 = new Event
                {
                    Title = "Understanding Ramadan",
                    Description = "A session on the spiritual dimensions of Ramadan.",
                    SpeakerName = speaker,
                    ImageUrl = null,
                    StartDateTime = now.AddDays(7),
                    EndDateTime = now.AddDays(7).AddHours(2),
                    MeetingLink = "https://meet.example.com/ramadan",
                    IsPublished = true
                };
                var event2 = new Event
                {
                    Title = "Intro to Quranic Sciences",
                    Description = "Overview of 'Ulum al-Quran.",
                    SpeakerName = "Ustadh Bilal",
                    ImageUrl = null,
                    StartDateTime = now.AddDays(14),
                    EndDateTime = now.AddDays(14).AddHours(1),
                    MeetingLink = "https://meet.example.com/quranic-sciences",
                    IsPublished = true
                };

                _context.Events.AddRange(event1, event2);
                await _context.SaveChangesAsync();

                // Register the student user for event1
                var eventStudentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "student@localhost");
                if (eventStudentUser != null)
                {
                    _context.EventRegistrations.Add(new EventRegistration
                    {
                        EventId = event1.Id,
                        UserId = eventStudentUser.Id,
                        QuestionText = "Will there be a Q&A session?",
                        Status = EventStatus.Approved
                    });
                    // Register student for second event as well
                    _context.EventRegistrations.Add(new EventRegistration
                    {
                        EventId = event2.Id,
                        UserId = eventStudentUser.Id,
                        QuestionText = "Is the session recorded?",
                        Status = EventStatus.Pending
                    });
                    await _context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();
        });
    }

    private Task CreateRoleSpecificEntity(Guid userId, UserRole role, ReligiousStatus religiousStatus = ReligiousStatus.BornMuslim)
    {
        switch (role)
        {
            case UserRole.Student:
            case UserRole.NonMuslim:
                if (!_context.Students.AsNoTracking().Any(s => s.UserId == userId))
                {
                    _context.Students.Add(new Student
                    {
                        UserId = userId,
                        EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
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
                        IsSuperAdmin = false
                    });
                }
                break;

            case UserRole.Preacher:
                if (!_context.Preachers.AsNoTracking().Any(p => p.UserId == userId))
                {
                    _context.Preachers.Add(new Preacher
                    {
                        UserId = userId,
                        JoinedDate = DateTime.UtcNow,
                        IsActive = true,
                        IsVerified = false
                    });
                }
                break;
        }

        return Task.CompletedTask;
    }
}
