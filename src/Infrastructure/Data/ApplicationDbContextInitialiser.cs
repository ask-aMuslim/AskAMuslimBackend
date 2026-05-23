using System.Globalization;
using System.Data.Common;
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

        logger.LogInformation(
            "InitialiseDatabaseAsync invoked for environment {EnvironmentName}.",
            app.Environment.EnvironmentName);
        await initialiser.InitialiseAsync();
        logger.LogInformation("Database migration step completed. Starting seed step.");
        await initialiser.SeedAsync();
        logger.LogInformation("Database initialization and seeding completed successfully.");
    }
}

public class ApplicationDbContextInitialiser
{
    private const int DefaultDevelopmentCommandTimeoutSeconds = 60;
    private const int DefaultProductionCommandTimeoutSeconds = 180;
    private const int DefaultDevelopmentInitializationTimeoutSeconds = 120;
    private const int DefaultProductionInitializationTimeoutSeconds = 300;

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
        var previousCommandTimeout = _context.Database.GetCommandTimeout();
        var commandTimeoutSeconds = GetCommandTimeoutSeconds();
        var initializationTimeoutSeconds = GetInitializationTimeoutSeconds();
        var connectionDiagnostics = GetConnectionDiagnostics();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(initializationTimeoutSeconds));

        try
        {
            _context.Database.SetCommandTimeout(commandTimeoutSeconds);

            _logger.LogInformation(
                "Checking EF migration history in environment {EnvironmentName}. Provider: {Provider}. DataSource: {DataSource}. Database: {Database}. CommandTimeoutSeconds: {CommandTimeoutSeconds}. InitializationTimeoutSeconds: {InitializationTimeoutSeconds}.",
                _env.EnvironmentName,
                connectionDiagnostics.Provider,
                connectionDiagnostics.DataSource,
                connectionDiagnostics.Database,
                commandTimeoutSeconds,
                initializationTimeoutSeconds);

            var appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync(timeoutCts.Token)).ToArray();

            var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(timeoutCts.Token)).ToArray();

            _logger.LogInformation(
                "EF migration history for database {Database}: {AppliedCount} applied migration(s), {PendingCount} pending migration(s). Latest applied migration: {LatestAppliedMigration}.",
                connectionDiagnostics.Database,
                appliedMigrations.Length,
                pendingMigrations.Length,
                appliedMigrations.LastOrDefault() ?? "<none>");

            if (pendingMigrations.Length == 0)
            {
                _logger.LogInformation(
                    "No pending database migrations were found for database {Database}.",
                    connectionDiagnostics.Database);
                return;
            }

            _logger.LogInformation(
                "Applying {MigrationCount} pending migration(s) to database {Database}: {MigrationNames}",
                pendingMigrations.Length,
                connectionDiagnostics.Database,
                string.Join(", ", pendingMigrations));

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await _context.Database.MigrateAsync(timeoutCts.Token);
            });

            _logger.LogInformation(
                "Database migrations applied successfully to database {Database}.",
                connectionDiagnostics.Database);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Database initialization timed out after {InitializationTimeoutSeconds} seconds in environment {EnvironmentName}. Verify connectivity, startup latency, and schema migration permissions for database {Database} on {DataSource}.",
                initializationTimeoutSeconds,
                _env.EnvironmentName,
                connectionDiagnostics.Database,
                connectionDiagnostics.DataSource);
            throw;
        }
        catch (Exception ex) when (ex is DbException or TimeoutException or InvalidOperationException)
        {
            _logger.LogError(
                ex,
                "Database initialization failed in environment {EnvironmentName}. Verify connection string 'AskAMuslimBackendDb', network access, and that the application identity can apply schema changes. Provider: {Provider}. DataSource: {DataSource}. Database: {Database}.",
                _env.EnvironmentName,
                connectionDiagnostics.Provider,
                connectionDiagnostics.DataSource,
                connectionDiagnostics.Database);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An unexpected error occurred while initialising the database in environment {EnvironmentName}.",
                _env.EnvironmentName);
            throw;
        }
        finally
        {
            _context.Database.SetCommandTimeout(previousCommandTimeout);
        }
    }

    public async Task SeedAsync()
    {
        var previousCommandTimeout = _context.Database.GetCommandTimeout();
        var commandTimeoutSeconds = GetCommandTimeoutSeconds();

        _logger.LogInformation("========== DATABASE SEEDING STARTED ==========");
        try
        {
            _context.Database.SetCommandTimeout(commandTimeoutSeconds);

            _logger.LogInformation(
                "Starting seeding process in environment {EnvironmentName} with command timeout {CommandTimeoutSeconds} seconds.",
                _env.EnvironmentName,
                commandTimeoutSeconds);
            await TrySeedAsync();
            _logger.LogInformation("========== DATABASE SEEDING COMPLETED ==========");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SEEDING FAILED: An error occurred during seeding. Details: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);

            if (!IsRunningUnderNSwag())
            {
                throw;
            }
        }
        finally
        {
            _context.Database.SetCommandTimeout(previousCommandTimeout);
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
            var updatedUsers = new List<User>();

            foreach (var (email, password, firstName, lastName, userRole, religousStatus) in userSeeds)
            {
                var existingUser = await _userManager.FindByEmailAsync(email);

                if (existingUser != null)
                {
                    _logger.LogInformation("User {Email} already exists, updating credentials and info from appsettings...", email);

                    // Update user information to match appsettings
                    var needsUpdate = false;

                    if (existingUser.FirstName != firstName)
                    {
                        _logger.LogInformation("Updating FirstName from '{OldName}' to '{NewName}'", existingUser.FirstName, firstName);
                        existingUser.FirstName = firstName;
                        needsUpdate = true;
                    }

                    if (existingUser.LastName != lastName)
                    {
                        _logger.LogInformation("Updating LastName from '{OldName}' to '{NewName}'", existingUser.LastName, lastName);
                        existingUser.LastName = lastName;
                        needsUpdate = true;
                    }

                    if (existingUser.Role != userRole)
                    {
                        _logger.LogInformation("Updating Role from '{OldRole}' to '{NewRole}'", existingUser.Role, userRole);
                        existingUser.Role = userRole;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        var updateResult = await _userManager.UpdateAsync(existingUser);
                        if (updateResult.Succeeded)
                        {
                            _logger.LogInformation("✅ User info updated successfully: {Email}", email);
                        }
                        else
                        {
                            _logger.LogError("❌ Failed to update user {Email}: {Errors}", email, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                        }
                    }

                    // Update password if it has changed
                    _logger.LogInformation("Updating password for {Email} to match appsettings...", email);
                    var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                    var passwordResult = await _userManager.ResetPasswordAsync(existingUser, token, password);

                    if (passwordResult.Succeeded)
                    {
                        _logger.LogInformation("✅ Password updated successfully for: {Email}", email);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to update password for {Email}: {Errors}", email, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    }

                    updatedUsers.Add(existingUser);
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

                // Grant SuperAdmin all permissions
                for (int i = 0; i < newUsers.Count; i++)
                {
                    var user = newUsers[i];
                    var isSuperAdmin = i == 0 && user.Role == UserRole.Admin;

                    if (isSuperAdmin)
                    {
                        _logger.LogInformation("🔑 Granting all permissions to SuperAdmin: {Email}", user.Email);
                        var userPermission = await _context.UserPermissions
                            .FirstOrDefaultAsync(up => up.UserId == user.Id);

                        if (userPermission == null)
                        {
                            _context.UserPermissions.Add(new UserPermission
                            {
                                UserId = user.Id,
                                Permissions = Permission.AllPermissions,
                                GrantedDate = DateTime.UtcNow,
                                Notes = "SuperAdmin - granted all permissions on initialization"
                            });
                        }
                        else
                        {
                            userPermission.Permissions = Permission.AllPermissions;
                            userPermission.GrantedDate = DateTime.UtcNow;
                            userPermission.Notes = "SuperAdmin - granted all permissions on initialization";
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ SuperAdmin permissions granted successfully");
                    }
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No new users were created");
            }

            // Handle updated users from appsettings
            if (updatedUsers.Count > 0)
            {
                _logger.LogInformation("🔧 Ensuring role-specific entities and permissions for {Count} updated users...", updatedUsers.Count);

                for (int i = 0; i < updatedUsers.Count; i++)
                {
                    var user = updatedUsers[i];
                    // First user (admin from AdminSeed) should be super admin
                    var isSuperAdmin = i == 0 && user.Role == UserRole.Admin;

                    if (user.Role == UserRole.Admin)
                    {
                        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == user.Id);

                        if (admin == null)
                        {
                            _logger.LogInformation("Creating Admin entity for existing user {Email} (IsSuperAdmin: {IsSuperAdmin})", user.Email, isSuperAdmin);
                            _context.Admins.Add(new Admin
                            {
                                UserId = user.Id,
                                JoinedDate = DateTime.UtcNow,
                                IsActive = true,
                                IsSuperAdmin = isSuperAdmin
                            });
                            await _context.SaveChangesAsync();
                        }
                        else if (admin.IsSuperAdmin != isSuperAdmin)
                        {
                            _logger.LogInformation("Updating IsSuperAdmin for {Email} from {OldValue} to {IsSuperAdmin}", user.Email, admin.IsSuperAdmin, isSuperAdmin);
                            admin.IsSuperAdmin = isSuperAdmin;
                            await _context.SaveChangesAsync();
                        }

                        // Ensure SuperAdmin has all permissions
                        if (isSuperAdmin)
                        {
                            _logger.LogInformation("🔑 Ensuring all permissions for updated SuperAdmin: {Email}", user.Email);
                            var userPermission = await _context.UserPermissions
                                .FirstOrDefaultAsync(up => up.UserId == user.Id);

                            if (userPermission == null)
                            {
                                _context.UserPermissions.Add(new UserPermission
                                {
                                    UserId = user.Id,
                                    Permissions = Permission.AllPermissions,
                                    GrantedDate = DateTime.UtcNow,
                                    Notes = "SuperAdmin - granted all permissions on credentials update"
                                });
                            }
                            else if (userPermission.Permissions != Permission.AllPermissions)
                            {
                                userPermission.Permissions = Permission.AllPermissions;
                                userPermission.GrantedDate = DateTime.UtcNow;
                                userPermission.Notes = "SuperAdmin - updated to all permissions on credentials update";
                            }

                            await _context.SaveChangesAsync();
                            _logger.LogInformation("✅ SuperAdmin permissions ensured successfully");
                        }
                    }
                }
            }

            // Ensure all existing SuperAdmins have all permissions
            _logger.LogInformation("🔍 Checking existing SuperAdmins for permissions...");
            var superAdmins = await _context.Admins
                .Where(a => a.IsSuperAdmin)
                .ToListAsync();

            foreach (var superAdmin in superAdmins)
            {
                var userPermission = await _context.UserPermissions
                    .FirstOrDefaultAsync(up => up.UserId == superAdmin.UserId);

                var needsUpdate = false;
                if (userPermission == null)
                {
                    _logger.LogInformation("🔑 Granting all permissions to existing SuperAdmin (UserId: {UserId})", superAdmin.UserId);
                    _context.UserPermissions.Add(new UserPermission
                    {
                        UserId = superAdmin.UserId,
                        Permissions = Permission.AllPermissions,
                        GrantedDate = DateTime.UtcNow,
                        Notes = "SuperAdmin - granted all permissions on system check"
                    });
                    needsUpdate = true;
                }
                else if (userPermission.Permissions != Permission.AllPermissions)
                {
                    _logger.LogInformation("🔄 Updating permissions to AllPermissions for existing SuperAdmin (UserId: {UserId})", superAdmin.UserId);
                    userPermission.Permissions = Permission.AllPermissions;
                    userPermission.GrantedDate = DateTime.UtcNow;
                    userPermission.Notes = "SuperAdmin - updated to all permissions on system check";
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    await _context.SaveChangesAsync();
                }
            }
            _logger.LogInformation("✅ SuperAdmin permissions check complete");

            // --- Seed RolePermissions for default role permissions ---
            _logger.LogInformation("🔐 Seeding default role permissions...");
            var rolePermissionsToSeed = new List<(UserRole Role, Permission Permissions, string Notes)>
            {
                (
                    UserRole.Admin,
                    Permission.ViewDashboard | Permission.ViewAnalytics | Permission.ViewAdmins |
                    Permission.CreateAdmin | Permission.EditAdmin | Permission.DeleteAdmin |
                    Permission.ManageAdminRoles | Permission.FullStudentAccess | Permission.FullAcademyAccess |
                    Permission.FullQAAccess | Permission.FullMuslimTubeAccess | Permission.ViewNotifications |
                    Permission.SendBulkNotifications | Permission.CreateNotificationTemplate |
                    Permission.ViewMeetingRequests | Permission.ManageMeetingRequests |
                    Permission.ViewInquiryRequests | Permission.ManageInquiryRequests |
                    Permission.FullEventAccess | Permission.ViewTags | Permission.CreateTag |
                    Permission.EditTag | Permission.DeleteTag | Permission.ViewSettings |
                    Permission.ViewReports | Permission.ManageRolesAndPermissions |
                    Permission.ViewFinancialData | Permission.ManageSystemBackup | Permission.ViewAuditLogs,
                    "Default permissions for Administrator role"
                ),
                (
                    UserRole.Instructor,
                    Permission.ViewEnrolledStudents | Permission.ViewStudentProgress |
                    Permission.SendNotifications | Permission.ViewStudentQuestions |
                    Permission.AnswerStudentQuestions | Permission.ManageAdminNotes |
                    Permission.SendDirectEmail | Permission.ViewAllCourses | Permission.CreateCourse |
                    Permission.EditOwnCourses | Permission.ViewLessons | Permission.CreateLesson |
                    Permission.EditLesson | Permission.DeleteLesson,
                    "Default permissions for Instructor role"
                ),
                (
                    UserRole.Preacher,
                    Permission.ViewStudentQuestions | Permission.ManageAdminNotes |
                    Permission.FullQAAccess | Permission.FullMuslimTubeAccess | Permission.FullEventAccess,
                    "Default permissions for Preacher role"
                ),
                (
                    UserRole.Student,
                    Permission.None,
                    "Default permissions for Student role (no special permissions)"
                ),
                (
                    UserRole.NonMuslim,
                    Permission.None,
                    "Default permissions for NonMuslim role (no special permissions)"
                )
            };

            foreach (var (role, permissions, notes) in rolePermissionsToSeed)
            {
                var existingRolePermission = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.Role == role);

                if (existingRolePermission == null)
                {
                    _logger.LogInformation("Creating default permissions for role: {Role}", role);
                    _context.RolePermissions.Add(new RolePermission
                    {
                        Role = role,
                        Permissions = permissions,
                        LastModifiedDate = DateTime.UtcNow,
                        Notes = notes
                    });
                }
                else
                {
                    _logger.LogInformation("Role permissions already exist for: {Role} (skipping)", role);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Role permissions seeding complete");

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

    private int GetCommandTimeoutSeconds()
    {
        var defaultTimeout = _env.IsProduction()
            ? DefaultProductionCommandTimeoutSeconds
            : DefaultDevelopmentCommandTimeoutSeconds;

        return Math.Max(
            30,
            _configuration.GetValue<int?>("DatabaseInitialization:CommandTimeoutSeconds") ?? defaultTimeout);
    }

    private int GetInitializationTimeoutSeconds()
    {
        var defaultTimeout = _env.IsProduction()
            ? DefaultProductionInitializationTimeoutSeconds
            : DefaultDevelopmentInitializationTimeoutSeconds;

        return Math.Max(
            60,
            _configuration.GetValue<int?>("DatabaseInitialization:InitializationTimeoutSeconds") ?? defaultTimeout);
    }

    private (string Provider, string DataSource, string Database) GetConnectionDiagnostics()
    {
        var connection = _context.Database.GetDbConnection();

        return (
            Provider: _context.Database.ProviderName ?? "Unknown",
            DataSource: string.IsNullOrWhiteSpace(connection.DataSource) ? "Unknown" : connection.DataSource,
            Database: string.IsNullOrWhiteSpace(connection.Database) ? "Unknown" : connection.Database);
    }

    private static bool IsRunningUnderNSwag()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(a => a.Contains("dotnet-nswag", StringComparison.OrdinalIgnoreCase) || a.Contains("nswag", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
