using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Infrastructure.Data;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AskAMuslimBackend.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPermissionService _permissionService;

    public IdentityService(
        ApplicationDbContext dbContext,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IFileStorageService fileStorageService,
        IPermissionService permissionService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _fileStorageService = fileStorageService;
        _permissionService = permissionService;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        return user?.Email;
    }

    //public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    //{
    //    var existingUser = await _userManager.FindByEmailAsync(userName);
    //    if (existingUser != null)
    //    {
    //        return (Result.Failure(new[] { "User with this email already exists." }), string.Empty);
    //    }

    //    var user = new User
    //    {
    //        UserName = userName,
    //        Email = userName,
    //        FirstName = string.Empty,
    //        LastName = string.Empty,
    //        Role = UserRole.NonMuslim,
    //        EmailConfirmed = true,
    //        IsActive = true,
    //        CreatedAt = DateTime.UtcNow
    //    };

    //    var result = await _userManager.CreateAsync(user, password);

    //    if (!result.Succeeded)
    //    {
    //        return (Result.Failure(result.Errors.Select(e => e.Description)), string.Empty);
    //    }

    //    await _userManager.AddToRoleAsync(user, UserRole.NonMuslim.ToString());

    //    return (Result.Success(), user.Id.ToString());
    //}

    public async Task<Result<AuthenticationResponse>> RegisterUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        UserRole role,
        ReligiousStatus religiousStatus = ReligiousStatus.BornMuslim,
        IFormFile? image = null,
        CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "User with this email already exists." });
        }

        // Upload image if provided
        string? imageUrl = null;
        if (image != null)
        {
            try
            {
                imageUrl = await _fileStorageService.UploadFileAsync(image, "profile-images", cancellationToken);
            }
            catch (Exception ex)
            {
                return Result<AuthenticationResponse>.Failure(new[] { $"Failed to upload image: {ex.Message}" });
            }
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            ReligionStatus = religiousStatus,
            ImageUrl = imageUrl,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
        }

        // Add user to role
        await _userManager.AddToRoleAsync(user, GetRoleName(role));

        // Create role-specific entity
        await CreateRoleSpecificEntity(user.Id, role);

        // Grant default permissions for the role
        var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(role.ToString());
        await _permissionService.SetUserPermissionsAsync(user.Id.ToString(), rolePermissions, null, $"Auto-granted on registration for role {role}");

        // Generate token
        var token = GenerateJwtToken(user);

        // Get token expiration from appsettings.json
        var tokenExpirationHours = _configuration.GetValue<int>("JwtSettings:TokenExpirationHours", 24);
        var expiresAt = DateTime.UtcNow.AddHours(tokenExpirationHours);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            ReligiousStatus = user.ReligionStatus,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthenticationResponse>.Success(authResponse);
    }

    #region Role Specific Entity Creation

    private Student CreateStudent(Guid userId)
    {
        return new Student
        {
            UserId = userId,
            EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        };
    }

    private Instructor CreateInstructor(Guid userId)
    {
        return new Instructor
        {
            UserId = userId,
            JoinedDate = DateTime.UtcNow,
            IsActive = true,
            IsVerified = false
        };
    }

    private Admin CreateAdmin(Guid userId)
    {
        return new Admin
        {
            UserId = userId,
            JoinedDate = DateTime.UtcNow,
            IsActive = true,
            IsSuperAdmin = false
        };
    }

    private Preacher CreatePreacher(Guid userId)
    {
        return new Preacher
        {
            UserId = userId,
            JoinedDate = DateTime.UtcNow,
            IsActive = true,
            IsVerified = false
        };
    }

    private async Task CreateRoleSpecificEntity(Guid userId, UserRole role)
    {
        switch (role)
        {
            case UserRole.Student:
            case UserRole.NonMuslim:
                _dbContext.Students.Add(CreateStudent(userId));
                break;

            case UserRole.Instructor:
                _dbContext.Instructors.Add(CreateInstructor(userId));
                break;

            case UserRole.Admin:
                _dbContext.Admins.Add(CreateAdmin(userId));
                break;

            case UserRole.Preacher:
                _dbContext.Preachers.Add(CreatePreacher(userId));
                break;
        }

        await _dbContext.SaveChangesAsync();
    }

    #endregion Role Specific Entity Creation

    public async Task<Result<AuthenticationResponse>> LoginUserAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Invalid email or password." });
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);

        if (!isPasswordValid)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Invalid email or password." });
        }

        if (!user.IsActive)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Your account has been deactivated. Please contact support for assistance." });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            ReligiousStatus = user.ReligionStatus,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthenticationResponse>.Success(authResponse);
    }

    // New: Google sign-in using ID token
    public async Task<Result<AuthenticationResponse>> LoginWithGoogleAsync(string idToken)
    {
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(googleClientId))
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Google ClientId not configured." });
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new List<string> { googleClientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
        }
        catch (InvalidJwtException ex)
        {
            return Result<AuthenticationResponse>.Failure(new[] { $"Invalid Google token: {ex.Message}" });
        }

        var email = payload.Email;
        var firstName = payload.GivenName ?? string.Empty;
        var lastName = payload.FamilyName ?? string.Empty;
        var imageUrl = payload.Picture; // Google profile picture

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = UserRole.Student, // default role, adjust as needed
                ReligionStatus = ReligiousStatus.NonMuslim, // default for OAuth users
                ImageUrl = imageUrl,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
            }

            await _userManager.AddToRoleAsync(user, GetRoleName(user.Role));

            // Create student entity for default role
            await CreateRoleSpecificEntity(user.Id, user.Role);
            // Grant default permissions for the role
            var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(user.Role.ToString());
            await _permissionService.SetUserPermissionsAsync(user.Id.ToString(), rolePermissions, null, $"Auto-granted on Facebook login for role {user.Role}");
        }

        if (!user.IsActive)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Your account has been deactivated. Please contact support for assistance." });
        }

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            ReligiousStatus = user.ReligionStatus,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthenticationResponse>.Success(authResponse);
    }

    // New: Facebook sign-in using access token
    public async Task<Result<AuthenticationResponse>> LoginWithFacebookAsync(string accessToken)
    {
        var appId = _configuration["Authentication:Facebook:AppId"];
        var appSecret = _configuration["Authentication:Facebook:AppSecret"];
        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Facebook AppId/AppSecret not configured." });
        }

        // Validate token and get user info from Graph API
        // Using app access token to debug the provided user token
        var appAccessToken = $"{appId}|{appSecret}";
        var http = _httpClientFactory.CreateClient();

        // Step 1: debug_token to validate
        var debugUri = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appAccessToken}";
        var debugResponse = await http.GetAsync(debugUri);
        if (!debugResponse.IsSuccessStatusCode)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Invalid Facebook token (debug call failed)." });
        }

        using var debugDoc = JsonDocument.Parse(await debugResponse.Content.ReadAsStringAsync());
        var dataEl = debugDoc.RootElement.GetProperty("data");
        var isValid = dataEl.GetProperty("is_valid").GetBoolean();
        if (!isValid)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Invalid Facebook token." });
        }

        var userId = dataEl.TryGetProperty("user_id", out var userIdProp) ? userIdProp.GetString() : null;

        // Step 2: fetch user profile
        var fields = "id,name,first_name,last_name,email";
        // email permission might not be granted; handle null email.
        var meUri = $"https://graph.facebook.com/v18.0/me?fields={fields}&access_token={accessToken}";
        var meResp = await http.GetAsync(meUri);
        if (!meResp.IsSuccessStatusCode)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Unable to fetch Facebook user profile." });
        }

        using var meDoc = JsonDocument.Parse(await meResp.Content.ReadAsStringAsync());
        var email = meDoc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        var firstName = meDoc.RootElement.TryGetProperty("first_name", out var fnProp) ? fnProp.GetString() : null;
        var lastName = meDoc.RootElement.TryGetProperty("last_name", out var lnProp) ? lnProp.GetString() : null;
        var fbId = meDoc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : userId;

        // Extract Facebook profile picture
        string? imageUrl = null;
        if (!string.IsNullOrWhiteSpace(fbId))
        {
            // Facebook profile picture URL format
            imageUrl = $"https://graph.facebook.com/{fbId}/picture?type=large";
        }

        // Fallback email if not present: create a synthetic email based on FB id and app domain
        if (string.IsNullOrWhiteSpace(email))
        {
            var audience = _configuration["JwtSettings:Audience"] ?? "askamuslim.local";
            email = $"fb_{fbId}@{audience}";
        }

        firstName ??= "";
        lastName ??= "";

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = UserRole.Student,
                ReligionStatus = ReligiousStatus.NonMuslim, // default for OAuth users
                ImageUrl = imageUrl,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
            }

            await _userManager.AddToRoleAsync(user, GetRoleName(user.Role));

            // Create student entity for default role
            await CreateRoleSpecificEntity(user.Id, user.Role);

            // Grant default permissions for the role
            var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(user.Role.ToString());
            await _permissionService.SetUserPermissionsAsync(user.Id.ToString(), rolePermissions, null, $"Auto-granted on Facebook login for role {user.Role}");
        }

        if (!user.IsActive)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Your account has been deactivated. Please contact support for assistance." });
        }

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            ReligiousStatus = user.ReligionStatus,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthenticationResponse>.Success(authResponse);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
        var issuer = jwtSettings["Issuer"] ?? "AskAMuslimBackend";
        var audience = jwtSettings["Audience"] ?? "AskAMuslimBackend";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.Role, GetRoleName(user.Role)),
            new("role", GetRoleName(user.Role)),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || !user.IsActive)
        {
            return false;
        }

        // Get user roles
        var userRoles = await _userManager.GetRolesAsync(user);

        // Administrators have all permissions
        if (userRoles.Contains(Domain.Constants.Roles.Administrator))
        {
            return true;
        }

        // Check custom user permissions from UserPermission table
        var userPermission = await _dbContext.UserPermissions
            .Where(up => up.UserId == userGuid)
            .FirstOrDefaultAsync();

        if (userPermission != null)
        {
            var hasPermission = CheckPermissionForPolicy(policyName, userPermission.Permissions);
            if (hasPermission)
            {
                return true;
            }
        }

        // Fallback to role-based authorization
        //TODO: Check this policyRoleMap => We currently get them from Database, not hardcoded, in case the superAdmin updated policy (Permission) for a specific Role
        var policyRoleMap = new Dictionary<string, string[]>
        {
            // System Policies
            { Domain.Constants.Policies.CanPurge, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.SuperAdminOnly, new[] { Domain.Constants.Roles.Administrator } },

            // Dashboard
            { Domain.Constants.Policies.ViewDashboard, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ViewAnalytics, new[] { Domain.Constants.Roles.Administrator } },

            // Admin Management
            { Domain.Constants.Policies.ViewAdmins, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ManageAdmins, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ManageAdminRoles, new[] { Domain.Constants.Roles.Administrator } },

            // User Management Policies
            { Domain.Constants.Policies.ManageAllUsers, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ViewAllUsers, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.AssignRoles, new[] { Domain.Constants.Roles.Administrator } },

            // Student Management Policies
            { Domain.Constants.Policies.ViewAllStudents, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ViewEnrolledStudents, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ManageStudents, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.SendNotifications, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ManageAdminNotes, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor, Domain.Constants.Roles.Preacher } },

            // Academy - Levels
            { Domain.Constants.Policies.ViewLevels, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ManageLevels, new[] { Domain.Constants.Roles.Administrator } },

            // Academy - Courses
            { Domain.Constants.Policies.ViewAllCourses, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ViewOwnCourses, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ManageAllCourses, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ManageOwnCourses, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.CreateCourse, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.PublishCourse, new[] { Domain.Constants.Roles.Administrator } },

            // Academy - Lessons & Quizzes
            { Domain.Constants.Policies.ViewLessons, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ManageLessons, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ViewQuizzes, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },
            { Domain.Constants.Policies.ManageQuizzes, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor } },

            // QA Management
            { Domain.Constants.Policies.ViewQA, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageQA, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ApproveQA, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },

            // MuslimTube
            { Domain.Constants.Policies.ViewMuslimTube, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageMuslimTube, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ApproveVideos, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },

            // Notifications
            { Domain.Constants.Policies.ViewNotifications, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.SendBulkNotifications, new[] { Domain.Constants.Roles.Administrator } },

            // Meeting & Inquiry Requests
            { Domain.Constants.Policies.ViewMeetingRequests, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageMeetingRequests, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ViewInquiryRequests, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageInquiryRequests, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },

            // Events
            { Domain.Constants.Policies.ViewEvents, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageEvents, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageEventRegistrations, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },

            // Tags
            { Domain.Constants.Policies.ViewTags, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Instructor, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageTags, new[] { Domain.Constants.Roles.Administrator } },

            // Settings & System
            { Domain.Constants.Policies.ViewSettings, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ManageSettings, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ViewReports, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ManageRolesAndPermissions, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ViewFinancialData, new[] { Domain.Constants.Roles.Administrator } },
            { Domain.Constants.Policies.ViewAuditLogs, new[] { Domain.Constants.Roles.Administrator } },

            // Legacy compatibility
            { Domain.Constants.Policies.ManageContent, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.PublishArticles, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageDawahContent, new[] { Domain.Constants.Roles.Administrator, Domain.Constants.Roles.Preacher } },
            { Domain.Constants.Policies.ManageSystem, new[] { Domain.Constants.Roles.Administrator } }
        };

        // Check if policy exists and user has any of the required roles
        if (policyRoleMap.TryGetValue(policyName, out var requiredRoles))
        {
            return userRoles.Any(role => requiredRoles.Contains(role));
        }

        // If policy is not found, deny access by default
        return false;
    }

    private bool CheckPermissionForPolicy(string policyName, Domain.Enums.Permission userPermissions)
    {
        // Map policies to permission flags
        var policyPermissionMap = new Dictionary<string, Domain.Enums.Permission>
        {
            // Dashboard
            { Domain.Constants.Policies.ViewDashboard, Domain.Enums.Permission.ViewDashboard },
            { Domain.Constants.Policies.ViewAnalytics, Domain.Enums.Permission.ViewAnalytics },

            // Admin Management
            { Domain.Constants.Policies.ViewAdmins, Domain.Enums.Permission.ViewAdmins },
            { Domain.Constants.Policies.ManageAdmins, Domain.Enums.Permission.CreateAdmin | Domain.Enums.Permission.EditAdmin | Domain.Enums.Permission.DeleteAdmin },
            { Domain.Constants.Policies.ManageAdminRoles, Domain.Enums.Permission.ManageAdminRoles },

            // Student Management
            { Domain.Constants.Policies.ViewAllStudents, Domain.Enums.Permission.ViewAllStudents },
            { Domain.Constants.Policies.ViewEnrolledStudents, Domain.Enums.Permission.ViewEnrolledStudents },
            { Domain.Constants.Policies.ManageStudents, Domain.Enums.Permission.EditStudent },
            { Domain.Constants.Policies.SendNotifications, Domain.Enums.Permission.SendNotifications },
            { Domain.Constants.Policies.ManageAdminNotes, Domain.Enums.Permission.ManageAdminNotes },

            // Academy - Levels
            { Domain.Constants.Policies.ViewLevels, Domain.Enums.Permission.ViewLevels },
            { Domain.Constants.Policies.ManageLevels, Domain.Enums.Permission.CreateLevel | Domain.Enums.Permission.EditLevel | Domain.Enums.Permission.DeleteLevel },

            // Academy - Courses
            { Domain.Constants.Policies.ViewAllCourses, Domain.Enums.Permission.ViewAllCourses },
            { Domain.Constants.Policies.ViewOwnCourses, Domain.Enums.Permission.ViewOwnCourses },
            { Domain.Constants.Policies.ManageAllCourses, Domain.Enums.Permission.EditAllCourses | Domain.Enums.Permission.DeleteCourse },
            { Domain.Constants.Policies.ManageOwnCourses, Domain.Enums.Permission.EditOwnCourses },
            { Domain.Constants.Policies.CreateCourse, Domain.Enums.Permission.CreateCourse },
            { Domain.Constants.Policies.PublishCourse, Domain.Enums.Permission.PublishCourse },

            // Academy - Lessons & Quizzes
            { Domain.Constants.Policies.ViewLessons, Domain.Enums.Permission.ViewLessons },
            { Domain.Constants.Policies.ManageLessons, Domain.Enums.Permission.CreateLesson | Domain.Enums.Permission.EditLesson | Domain.Enums.Permission.DeleteLesson },
            { Domain.Constants.Policies.ViewQuizzes, Domain.Enums.Permission.ViewQuizzes },
            { Domain.Constants.Policies.ManageQuizzes, Domain.Enums.Permission.CreateQuiz | Domain.Enums.Permission.EditQuiz | Domain.Enums.Permission.DeleteQuiz },

            // QA
            { Domain.Constants.Policies.ViewQA, Domain.Enums.Permission.ViewQA },
            { Domain.Constants.Policies.ManageQA, Domain.Enums.Permission.CreateQA | Domain.Enums.Permission.EditQA | Domain.Enums.Permission.DeleteQA },
            { Domain.Constants.Policies.ApproveQA, Domain.Enums.Permission.ApproveQA },

            // MuslimTube
            { Domain.Constants.Policies.ViewMuslimTube, Domain.Enums.Permission.ViewMuslimTube },
            { Domain.Constants.Policies.ManageMuslimTube, Domain.Enums.Permission.CreateVideo | Domain.Enums.Permission.EditVideo | Domain.Enums.Permission.DeleteVideo },
            { Domain.Constants.Policies.ApproveVideos, Domain.Enums.Permission.ApproveVideo },

            // Notifications
            { Domain.Constants.Policies.ViewNotifications, Domain.Enums.Permission.ViewNotifications },
            { Domain.Constants.Policies.SendBulkNotifications, Domain.Enums.Permission.SendBulkNotifications },

            // Meeting & Inquiry
            { Domain.Constants.Policies.ViewMeetingRequests, Domain.Enums.Permission.ViewMeetingRequests },
            { Domain.Constants.Policies.ManageMeetingRequests, Domain.Enums.Permission.ManageMeetingRequests },
            { Domain.Constants.Policies.ViewInquiryRequests, Domain.Enums.Permission.ViewInquiryRequests },
            { Domain.Constants.Policies.ManageInquiryRequests, Domain.Enums.Permission.ManageInquiryRequests },

            // Events
            { Domain.Constants.Policies.ViewEvents, Domain.Enums.Permission.ViewEvents },
            { Domain.Constants.Policies.ManageEvents, Domain.Enums.Permission.CreateEvent | Domain.Enums.Permission.EditEvent | Domain.Enums.Permission.DeleteEvent },
            { Domain.Constants.Policies.ManageEventRegistrations, Domain.Enums.Permission.ManageEventRegistrations },

            // Tags
            { Domain.Constants.Policies.ViewTags, Domain.Enums.Permission.ViewTags },
            { Domain.Constants.Policies.ManageTags, Domain.Enums.Permission.CreateTag | Domain.Enums.Permission.EditTag | Domain.Enums.Permission.DeleteTag },

            // Settings & System
            { Domain.Constants.Policies.ViewSettings, Domain.Enums.Permission.ViewSettings },
            { Domain.Constants.Policies.ManageSettings, Domain.Enums.Permission.ViewSettings },
            { Domain.Constants.Policies.ViewReports, Domain.Enums.Permission.ViewReports },
            { Domain.Constants.Policies.ManageRolesAndPermissions, Domain.Enums.Permission.ManageRolesAndPermissions },
            { Domain.Constants.Policies.ViewFinancialData, Domain.Enums.Permission.ViewFinancialData },
            { Domain.Constants.Policies.ViewAuditLogs, Domain.Enums.Permission.ViewAuditLogs }
        };

        if (policyPermissionMap.TryGetValue(policyName, out var requiredPermission))
        {
            return (userPermissions & requiredPermission) == requiredPermission;
        }

        return false;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return Result.Failure(new[] { "Invalid user ID format." });
        }

        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(User user)
    {
        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return Result.Failure(result.Errors.Select(e => e.Description));
        }

        return Result.Success();
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
