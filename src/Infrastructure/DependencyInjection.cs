using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Infrastructure.Data;
using AskAMuslimBackend.Infrastructure.Data.Interceptors;
using AskAMuslimBackend.Infrastructure.Identity;
using AskAMuslimBackend.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
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
            options.UseSqlServer(connectionString);
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
            // System Policies
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.SuperAdminOnly, policy => policy.RequireRole(Roles.Administrator));
            
            // User Management Policies
            options.AddPolicy(Policies.ManageAllUsers, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ViewAllUsers, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.AssignRoles, policy => 
                policy.RequireRole(Roles.Administrator));
            
            // Student Management Policies
            options.AddPolicy(Policies.ViewAllStudents, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ViewEnrolledStudents, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageStudents, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.SendNotifications, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageAdminNotes, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor, Roles.Preacher));
            
            // Course Management Policies
            options.AddPolicy(Policies.ViewAllCourses, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.ManageAllCourses, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageOwnCourses, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            options.AddPolicy(Policies.CreateCourse, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Instructor));
            
            // Content Management Policies
            options.AddPolicy(Policies.ManageContent, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageQA, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.PublishArticles, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            options.AddPolicy(Policies.ManageDawahContent, policy => 
                policy.RequireRole(Roles.Administrator, Roles.Preacher));
            
            // System Policies
            options.AddPolicy(Policies.ViewReports, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageSystem, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ViewFinancialData, policy => 
                policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.ManageRolesAndPermissions, policy => 
                policy.RequireRole(Roles.Administrator));
        });
    }
}
