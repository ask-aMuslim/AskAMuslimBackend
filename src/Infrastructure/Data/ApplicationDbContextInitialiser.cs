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
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContextInitialiser>>();
        
        logger.LogInformation("🚀 InitialiseDatabaseAsync called - starting database initialization...");
        await initialiser.InitialiseAsync();
        logger.LogInformation("✅ InitialiseAsync completed - calling SeedAsync...");
        await initialiser.SeedAsync();
        logger.LogInformation("✅ Database initialization and seeding complete!");
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
        _logger.LogInformation("========== DATABASE SEEDING STARTED ==========");
        try
        {
            // Check if database can be connected before seeding
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogError("❌ SEEDING FAILED: Cannot connect to database. Skipping seeding.");
                return;
            }

            _logger.LogInformation("✅ Database connection successful. Starting seeding process...");
            await TrySeedAsync();
            _logger.LogInformation("========== DATABASE SEEDING COMPLETED ==========");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SEEDING FAILED: An error occurred during seeding. Details: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
            // Do not rethrow here to avoid failing build tooling (e.g., NSwag) which executes the app during generation.
            // In a normal run, seeding issues should be investigated and resolved.
        }
    }

    public async Task TrySeedAsync()
    {
        _logger.LogInformation("📌 TrySeedAsync method started");
        // Use execution strategy to wrap transaction for retry compatibility
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            _logger.LogInformation("🔄 Starting transaction for seeding...");
            await using var transaction = await _context.Database.BeginTransactionAsync();

            // --- Seed Identity Roles ---
            _logger.LogInformation("👥 Seeding Identity Roles...");
            var roles = new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Student, Domain.Constants.Roles.Instructor, Domain.Constants.Roles.NonMuslim, Domain.Constants.Roles.Preacher };
            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }

            // --- Default Users ---
            // Read admin configuration from appsettings
            _logger.LogInformation("📖 Reading admin configuration from appsettings...");
            var adminEmail = _configuration["AdminSeed:Email"] ?? "administrator@localhost";
            var adminPassword = _configuration["AdminSeed:Password"] ?? "Administrator1!";
            var adminFirstName = _configuration["AdminSeed:FirstName"] ?? "Admin";
            var adminLastName = _configuration["AdminSeed:LastName"] ?? "User";
            _logger.LogInformation("📧 Admin Email from config: {Email}", adminEmail);
            _logger.LogInformation("👤 Admin Name from config: {FirstName} {LastName}", adminFirstName, adminLastName);

            // Only seed SuperAdmin user from appsettings.json
            var userSeeds = new List<(string Email, string Password, string FirstName, string LastName, UserRole UserRole, ReligiousStatus religion)>
        {
            (adminEmail, adminPassword, adminFirstName, adminLastName, UserRole.Admin, ReligiousStatus.BornMuslim)
            // Commented out test users - uncomment for development/testing
            // ("student@localhost", "Student1!", "John", "Student", UserRole.Student, ReligiousStatus.RevertedMuslim),
            // ("student2@localhost", "Student2!", "Sara", "Learner", UserRole.Student, ReligiousStatus.RevertedMuslim),
            // ("nonmuslim@localhost", "NonMuslim1!", "Jane", "NonMuslim", UserRole.NonMuslim, ReligiousStatus.NonMuslim),
            // ("nonmuslim2@localhost", "NonMuslim2!", "Liam", "Guest", UserRole.NonMuslim, ReligiousStatus.NonMuslim),
            // ("instructor@localhost", "Instructor1!", "Bob", "Instructor", UserRole.Instructor, ReligiousStatus.BornMuslim),
            // ("instructor2@localhost", "Instructor2!", "Fatima", "Khan", UserRole.Instructor, ReligiousStatus.BornMuslim),
            // ("instructor3@localhost", "Instructor3!", "Omar", "Saeed", UserRole.Instructor, ReligiousStatus.BornMuslim),
            // ("preacher@localhost", "Preacher1!", "Ahmed", "Preacher", UserRole.Preacher, ReligiousStatus.BornMuslim),
            // ("preacher2@localhost", "Preacher2!", "Ibrahim", "Saleh", UserRole.Preacher, ReligiousStatus.BornMuslim),
            // ("admin2@localhost", "Admin2!", "Alice", "Adminson", UserRole.Admin, ReligiousStatus.BornMuslim)
        };

            var newUsers = new List<User>();
            foreach (var (email, password, firstName, lastName, userRole, religousStatus) in userSeeds)
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    _logger.LogInformation("User {Email} already exists, skipping creation.", email);
                    continue;
                }

                _logger.LogInformation("Creating SuperAdmin user: {Email}", email);
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
                    _logger.LogInformation("✅ User created successfully: {Email}", email);
                    await _userManager.AddToRoleAsync(user, GetRoleName(userRole));
                    _logger.LogInformation("✅ Added user {Email} to role: {Role}", email, GetRoleName(userRole));
                    newUsers.Add(user);
                }
                else
                {
                    _logger.LogError("❌ Failed to create user {Email}: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            if (newUsers.Count > 0)
            {
                _logger.LogInformation("🔧 Creating role-specific entities for {Count} new users...", newUsers.Count);
                // Create role-specific entities without saving each time; save once after.
                for (int i = 0; i < newUsers.Count; i++)
                {
                    var user = newUsers[i];
                    // First user (admin from AdminSeed) should be super admin
                    var isSuperAdmin = i == 0 && user.Role == UserRole.Admin;
                    _logger.LogInformation("Creating {Role} entity for {Email} (IsSuperAdmin: {IsSuperAdmin})", user.Role, user.Email, isSuperAdmin);
                    await CreateRoleSpecificEntity(user.Id, user.Role, user.ReligionStatus ?? ReligiousStatus.BornMuslim, isSuperAdmin);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Role-specific entities saved successfully");
            }
            else
            {
                _logger.LogWarning("⚠️ No new users were created (they may already exist)");
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

            // Seed LastLoginAt only for SuperAdmin
            var generalLoginSeeds = new List<(string Email, DateTime LastLogin)>
            {
                (adminEmail, DateTime.UtcNow.AddDays(-1)) // within last 7 days
                // Commented out test users login data
                // ("admin2@localhost", DateTime.UtcNow.AddDays(-40)),
                // ("student@localhost", DateTime.UtcNow),
                // ("student2@localhost", DateTime.UtcNow.AddDays(-10)),
                // ("instructor@localhost", DateTime.UtcNow.AddDays(-3)),
                // ("instructor2@localhost", DateTime.UtcNow.AddDays(-20)),
                // ("instructor3@localhost", DateTime.UtcNow.AddDays(-2)),
                // ("preacher@localhost", DateTime.UtcNow.AddDays(-5)),
                // ("preacher2@localhost", DateTime.UtcNow.AddDays(-15)),
                // ("nonmuslim@localhost", DateTime.UtcNow.AddDays(-8))
            };

            foreach (var (email, lastLogin) in generalLoginSeeds)
            {
                var u = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
                if (u != null)
                {
                    u.LastLoginAt = lastLogin;
                    _context.Users.Update(u);

                    // Also set Admin.LastLoginDate if an Admin entity exists for user
                    var adminEntity = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == u.Id);
                    if (adminEntity != null)
                    {
                        adminEntity.LastLoginDate = lastLogin;
                        _context.Admins.Update(adminEntity);
                    }
                }
            }

            await _context.SaveChangesAsync();

            // --- Ensure StudentProfiles exist for all Students (including Non-Muslims) ---
            var allStudents = await _context.Students.AsNoTracking().ToListAsync();
            var profilesToAdd = new List<StudentProfile>();
            foreach (var student in allStudents)
            {
                var existingProfile = await _context.StudentProfiles.AsNoTracking().FirstOrDefaultAsync(sp => sp.StudentId == student.Id);
                if (existingProfile != null) continue;

                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == student.UserId);
                var isMuslim = false;
                var isNewMuslim = false;
                DateOnly? conversionDate = null;
                string? oldReligion = null;

                if (user != null && user.ReligionStatus.HasValue)
                {
                    switch (user.ReligionStatus.Value)
                    {
                        case ReligiousStatus.BornMuslim:
                            isMuslim = true;
                            break;
                        case ReligiousStatus.RevertedMuslim:
                            isMuslim = true;
                            isNewMuslim = true;
                            conversionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
                            oldReligion = "Unknown";
                            break;
                        case ReligiousStatus.NonMuslim:
                        default:
                            isMuslim = false;
                            break;
                    }
                }

                profilesToAdd.Add(new StudentProfile
                {
                    StudentId = student.Id,
                    IsMuslim = isMuslim,
                    IsNewMuslim = isNewMuslim,
                    DateOfIslamConversion = conversionDate,
                    OldReligion = oldReligion
                });
            }

            if (profilesToAdd.Count > 0)
            {
                _context.StudentProfiles.AddRange(profilesToAdd);
                await _context.SaveChangesAsync();
            }

            // // Commented out: Student seeding is disabled
            // var studentUser = await _userManager.FindByEmailAsync("student@localhost");
            // if (studentUser is null)
            // {
            //     _logger.LogWarning("Student user not found; skipping student-related seed data.");
            //     await transaction.CommitAsync();
            //     return;
            // }

            // var studentId = await _context.Students
            //     .AsNoTracking()
            //     .Where(s => s.UserId == studentUser.Id)
            //     .Select(s => (Guid?)s.Id)
            //     .FirstOrDefaultAsync();

            // if (studentId is null)
            // {
            //     _logger.LogWarning("Student entity not found for user; skipping student-related seed data.");
            //     await transaction.CommitAsync();
            //     return;
            // }

            // var studentEntityId = studentId.Value;

            // // --- Todo List (DISABLED) ---
            // if (!await _context.TodoLists.AsNoTracking().AnyAsync())
            // {
            //     _context.TodoLists.Add(new TodoList
            //     {
            //         Title = "Todo List",
            //         Items =
            //     {
            //         new TodoItem { Title = "Make a todo list ??" },
            //         new TodoItem { Title = "Check off the first item ?" },
            //         new TodoItem { Title = "Realise you've already done two things on the list! ??"},
            //         new TodoItem { Title = "Reward yourself with a nice, long nap ??" },
            //     }
            //     });

            //     await _context.SaveChangesAsync();
            // }

            // // --- Tags (DISABLED) ---
            // if (!await _context.Tags.AsNoTracking().AnyAsync())
            // {
            //     var tags = new List<Tag>
            // {
            //     new Tag { Title = "Islamic Studies", Description = "Foundational topics on Islam." },
            //     new Tag { Title = "Quran", Description = "Quranic studies and recitation." },
            //     new Tag { Title = "Hadith", Description = "Prophetic traditions and explanations." },
            //     new Tag { Title = "Beginner Friendly", Description = "Suitable for newcomers." }
            // };
            //     _context.Tags.AddRange(tags);
            //     await _context.SaveChangesAsync();
            // }

            // // --- Levels (DISABLED) ---
            // if (!await _context.Levels.AsNoTracking().AnyAsync())
            // {
            //     var levels = new List<Level>
            // {
            //     new Level { Title = "Beginner", Description = "Introductory level for new learners.", Difficulty = LevelDifficulty.Beginner, Order = 1 },
            //     new Level { Title = "Intermediate", Description = "For learners with some background.", Difficulty = LevelDifficulty.Intermediate, Order = 2 },
            //     new Level { Title = "Advanced", Description = "In-depth topics and advanced study.", Difficulty = LevelDifficulty.Advanced, Order = 3 }
            // };

            //     _context.Levels.AddRange(levels);
            //     await _context.SaveChangesAsync();
            // }

            _logger.LogInformation("💾 Committing transaction...");
            await transaction.CommitAsync();
            _logger.LogInformation("✅ Transaction committed successfully!");
        });

        // All other demo data seeding has been disabled (Courses, Lessons, Quizzes, Student data, QAs, Events, etc.)
        // Uncomment sections as needed for development/testing
    }

    private Task CreateRoleSpecificEntity(Guid userId, UserRole role, ReligiousStatus religiousStatus = ReligiousStatus.BornMuslim, bool isSuperAdmin = false)
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
                        IsSuperAdmin = isSuperAdmin
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

    private static string GetRoleName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => Domain.Constants.Roles.Administrator,
            UserRole.Student => Domain.Constants.Roles.Student,
            UserRole.Instructor => Domain.Constants.Roles.Instructor,
            UserRole.Preacher => Domain.Constants.Roles.Preacher,
            UserRole.NonMuslim => Domain.Constants.Roles.NonMuslim,
            _ => role.ToString()
        };
    }
}
