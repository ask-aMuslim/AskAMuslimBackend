using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
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

        builder.Services.AddAuthorization(options =>
        {
            // System Policies - Admin only
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.SuperAdminOnly, policy => policy.RequireRole(Roles.Administrator));
            
            // Dashboard - All admin roles can view
            options.AddPolicy(Policies.ViewDashboard, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor, Roles.Preacher));
            options.AddPolicy(Policies.ViewAnalytics, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Admin Management - Admin only
            options.AddPolicy(Policies.ViewAdmins, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageAdmins, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageAdminRoles, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // User Management Policies - Admin only
            options.AddPolicy(Policies.ManageAllUsers, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ViewAllUsers, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.AssignRoles, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Student Management Policies
            options.AddPolicy(Policies.ViewAllStudents, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ViewEnrolledStudents, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageStudents, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.SendNotifications, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageAdminNotes, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor, Roles.Preacher));
            
            // Academy - Levels
            options.AddPolicy(Policies.ViewLevels, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageLevels, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Academy - Courses
            options.AddPolicy(Policies.ViewAllCourses, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ViewOwnCourses, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageAllCourses, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageOwnCourses, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.CreateCourse, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.PublishCourse, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Academy - Lessons & Quizzes
            options.AddPolicy(Policies.ViewLessons, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageLessons, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ViewQuizzes, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageQuizzes, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            
            // QA Management
            options.AddPolicy(Policies.ViewQA, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageQA, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ApproveQA, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            
            // MuslimTube
            options.AddPolicy(Policies.ViewMuslimTube, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageMuslimTube, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ApproveVideos, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            
            // Notifications
            options.AddPolicy(Policies.ViewNotifications, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor, Roles.Preacher));
            options.AddPolicy(Policies.SendBulkNotifications, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Meeting & Inquiry Requests
            options.AddPolicy(Policies.ViewMeetingRequests, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageMeetingRequests, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ViewInquiryRequests, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageInquiryRequests, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            
            // Events
            options.AddPolicy(Policies.ViewEvents, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageEvents, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageEventRegistrations, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            
            // Tags
            options.AddPolicy(Policies.ViewTags, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor, Roles.Preacher));
            options.AddPolicy(Policies.ManageTags, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Settings & System - Admin only
            options.AddPolicy(Policies.ViewSettings, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageSettings, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ViewReports, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageRolesAndPermissions, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ViewFinancialData, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ViewAuditLogs, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Legacy compatibility
            options.AddPolicy(Policies.ManageContent, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.PublishArticles, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageDawahContent, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageSystem, policy => 
                policy.RequireRole(Roles.Administrator));
        });
    }
}
