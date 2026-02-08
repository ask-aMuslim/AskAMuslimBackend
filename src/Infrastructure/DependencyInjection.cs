using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Infrastructure.Data;
using AskAMuslimBackend.Infrastructure.Data.Interceptors;
using AskAMuslimBackend.Infrastructure.Identity;
using AskAMuslimBackend.Infrastructure.Services;
using AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AskAMuslimBackend.Application.MuslimTube.Queries.Caching;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("AskAMuslimBackendDb");
        Guard.Against.Null(connectionString, message: "Connection string 'AskAMuslimBackendDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlServerOptions.CommandTimeout(60);
            });
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.Services.AddHttpClient();
        builder.Services.AddMemoryCache();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        // Configure ASP.NET Core Identity
        builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;

            // SignIn settings
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        builder.Services.AddScoped<IYouTubeService, YouTubeService>();
        builder.Services.AddSingleton<IMuslimTubeCache, MuslimTubeMemoryCache>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<IContentRenderingService, ContentRenderingService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IOtpService, OtpService>();
        builder.Services.AddScoped<IEventNotificationService, EventNotificationService>();

        // Configure Firebase Admin SDK
        var firebaseCredentialPath = builder.Configuration["Firebase:CredentialPath"];
        if (!string.IsNullOrEmpty(firebaseCredentialPath))
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, firebaseCredentialPath);
            if (File.Exists(fullPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(fullPath)
                });
            }
        }

        builder.Services.AddScoped<IFirebaseMessagingService, FirebaseMessagingService>();

        // Configure Hangfire
        GlobalConfiguration.Configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            });

        builder.Services.AddScoped<IBackgroundJobService, BackgroundNotificationJobService>();
        builder.Services.AddScoped<NotificationJobExecutor>();

        // Configure JWT Authentication
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyForDevelopmentPurposeOnly123456!";
        var issuer = jwtSettings["Issuer"] ?? "AskAMuslimBackend";
        var audience = jwtSettings["Audience"] ?? "AskAMuslimBackend";

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };
        });

        builder.Services.AddAuthorizationBuilder();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IPermissionService, PermissionService>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        builder.Services.AddAuthorization(options =>
        {
            // System Policies - Admin only
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.SuperAdminOnly, policy => policy.RequireRole(Roles.Administrator));
            
            // Dashboard
            options.AddPolicy(Policies.ViewDashboard, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewDashboard)));
            options.AddPolicy(Policies.ViewAnalytics, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewAnalytics)));
            
            // Admin Management
            options.AddPolicy(Policies.ViewAdmins, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewAdmins)));
            options.AddPolicy(Policies.ManageAdmins, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateAdmin, Permission.EditAdmin, Permission.DeleteAdmin }, requireAll: false)));
            options.AddPolicy(Policies.ManageAdminRoles, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ManageAdminRoles)));
            
            // User Management Policies
            options.AddPolicy(Policies.ManageAllUsers, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.ViewAllStudents, Permission.EditStudent, Permission.DeleteStudent }, requireAll: false)));
            options.AddPolicy(Policies.ViewAllUsers, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewAllStudents)));
            options.AddPolicy(Policies.AssignRoles, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ManageAdminRoles)));
            
            // Student Management Policies
            options.AddPolicy(Policies.ViewAllStudents, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewAllStudents)));
            options.AddPolicy(Policies.ViewEnrolledStudents, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewEnrolledStudents)));
            options.AddPolicy(Policies.ManageStudents, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.EditStudent, Permission.DeleteStudent }, requireAll: false)));
            options.AddPolicy(Policies.SendNotifications, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.SendNotifications)));
            options.AddPolicy(Policies.ManageAdminNotes, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ManageAdminNotes)));
            
            // Academy - Levels
            options.AddPolicy(Policies.ViewLevels, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewLevels)));
            options.AddPolicy(Policies.ManageLevels, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateLevel, Permission.EditLevel, Permission.DeleteLevel }, requireAll: false)));
            
            // Academy - Courses
            options.AddPolicy(Policies.ViewAllCourses, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewAllCourses)));
            options.AddPolicy(Policies.ViewOwnCourses, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewOwnCourses)));
            options.AddPolicy(Policies.ManageAllCourses, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.EditAllCourses)));
            options.AddPolicy(Policies.ManageOwnCourses, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.EditOwnCourses)));
            options.AddPolicy(Policies.CreateCourse, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.CreateCourse)));
            options.AddPolicy(Policies.PublishCourse, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.PublishCourse)));
            
            // Academy - Lessons & Quizzes
            options.AddPolicy(Policies.ViewLessons, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewLessons)));
            options.AddPolicy(Policies.ManageLessons, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateLesson, Permission.EditLesson, Permission.DeleteLesson }, requireAll: false)));
            options.AddPolicy(Policies.ViewQuizzes, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewQuizzes)));
            options.AddPolicy(Policies.ManageQuizzes, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateQuiz, Permission.EditQuiz, Permission.DeleteQuiz }, requireAll: false)));
            
            // QA Management
            options.AddPolicy(Policies.ViewQA, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewQA)));
            options.AddPolicy(Policies.ManageQA, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateQA, Permission.EditQA, Permission.DeleteQA }, requireAll: false)));
            options.AddPolicy(Policies.ApproveQA, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ApproveQA)));
            
            // MuslimTube
            options.AddPolicy(Policies.ViewMuslimTube, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewMuslimTube)));
            options.AddPolicy(Policies.ManageMuslimTube, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateVideo, Permission.EditVideo, Permission.DeleteVideo }, requireAll: false)));
            options.AddPolicy(Policies.ApproveVideos, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ApproveVideo)));
            
            // Notifications
            options.AddPolicy(Policies.ViewNotifications, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewNotifications)));
            options.AddPolicy(Policies.SendBulkNotifications, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.SendBulkNotifications)));
            
            // Meeting & Inquiry Requests
            options.AddPolicy(Policies.ViewMeetingRequests, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewMeetingRequests)));
            options.AddPolicy(Policies.ManageMeetingRequests, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ManageMeetingRequests)));
            options.AddPolicy(Policies.ViewInquiryRequests, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewInquiryRequests)));
            options.AddPolicy(Policies.ManageInquiryRequests, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ManageInquiryRequests)));
            
            // Events
            options.AddPolicy(Policies.ViewEvents, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewEvents)));
            options.AddPolicy(Policies.ManageEvents, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateEvent, Permission.EditEvent, Permission.DeleteEvent }, requireAll: false)));
            options.AddPolicy(Policies.ManageEventRegistrations, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ManageEventRegistrations)));
            
            // Tags
            options.AddPolicy(Policies.ViewTags, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewTags)));
            options.AddPolicy(Policies.ManageTags, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.CreateTag, Permission.EditTag, Permission.DeleteTag }, requireAll: false)));
            
            // Settings & System
            options.AddPolicy(Policies.ViewSettings, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewSettings)));
            options.AddPolicy(Policies.ManageSettings, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewSettings))); // Using ViewSettings as ManageSettings is an alias
            options.AddPolicy(Policies.ViewReports, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.ViewDashboard, Permission.ViewAnalytics }, requireAll: false)));
            options.AddPolicy(Policies.ManageRolesAndPermissions, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ManageAdminRoles)));
            options.AddPolicy(Policies.ViewOwnPermissions, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewDashboard)));
            options.AddPolicy(Policies.ViewFinancialData, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.ViewDashboard, Permission.ViewAnalytics }, requireAll: false)));
            options.AddPolicy(Policies.ViewAuditLogs, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewSettings)));
            
            // Legacy compatibility
            options.AddPolicy(Policies.ManageContent, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.ViewQA, Permission.ViewMuslimTube }, requireAll: false)));
            options.AddPolicy(Policies.PublishArticles, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.ApproveQA, Permission.PublishCourse }, requireAll: false)));
            options.AddPolicy(Policies.ManageDawahContent, policy => 
                policy.AddRequirements(new PermissionRequirement(new[] { Permission.ViewQA, Permission.ViewMuslimTube }, requireAll: false)));
            options.AddPolicy(Policies.ManageSystem, policy => 
                policy.AddRequirements(new PermissionRequirement(Permission.ViewSettings)));
        });
    }
}
