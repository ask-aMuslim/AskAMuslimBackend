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
    private readonly ITokenBlacklistService _tokenBlacklistService;

    public IdentityService(
        ApplicationDbContext dbContext,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IFileStorageService fileStorageService,
        IPermissionService permissionService,
        ITokenBlacklistService tokenBlacklistService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _fileStorageService = fileStorageService;
        _permissionService = permissionService;
        _tokenBlacklistService = tokenBlacklistService;
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
        CancellationToken cancellationToken = default,
        bool allowAdminCreation = false)
    {
        // SECURITY: Prevent Admin role registration through public registration
        // Admins can only be created via:
        // 1. Database seeding (creates SuperAdmin)
        // 2. CreateAdminCommand by existing admin (creates regular Admin) - uses allowAdminCreation=true
        if (role == UserRole.Admin && !allowAdminCreation)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Cannot register as Admin. Admins must be created by existing administrators." });
        }

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

        // Create role-specific entity (regular users, NOT SuperAdmin)
        // SuperAdmins are only created through database seeding, not registration
        await CreateRoleSpecificEntity(user.Id, role, isSuperAdmin: false);

        // Grant default role permissions (regular users get role-based permissions)
        var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(role.ToString());
        await _permissionService.SetUserPermissionsAsync(user.Id.ToString(), rolePermissions, null, $"Auto-granted on registration for role {role}");

        // Generate tokens and response
        var authResponse = await CreateAuthenticationResponseAsync(user);

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

    private Admin CreateAdmin(Guid userId, bool isSuperAdmin = false)
    {
        return new Admin
        {
            UserId = userId,
            JoinedDate = DateTime.UtcNow,
            IsActive = true,
            IsSuperAdmin = isSuperAdmin
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

    private async Task<bool> CreateRoleSpecificEntity(Guid userId, UserRole role, bool isSuperAdmin = false)
    {
        bool createdAsSuperAdmin = false;

        switch (role)
        {
            case UserRole.Student:
            case UserRole.NonMuslim:
                // Check if student already exists to avoid duplicate errors
                var existingStudent = await _dbContext.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (existingStudent == null)
                {
                    _dbContext.Students.Add(CreateStudent(userId));
                }
                break;

            case UserRole.Instructor:
                var existingInstructor = await _dbContext.Instructors.FirstOrDefaultAsync(i => i.UserId == userId);
                if (existingInstructor == null)
                {
                    _dbContext.Instructors.Add(CreateInstructor(userId));
                }
                break;

            case UserRole.Admin:
                var existingAdmin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
                if (existingAdmin == null)
                {
                    _dbContext.Admins.Add(CreateAdmin(userId, isSuperAdmin));
                    createdAsSuperAdmin = isSuperAdmin;
                }
                break;

            case UserRole.Preacher:
                var existingPreacher = await _dbContext.Preachers.FirstOrDefaultAsync(p => p.UserId == userId);
                if (existingPreacher == null)
                {
                    _dbContext.Preachers.Add(CreatePreacher(userId));
                }
                break;
        }

        await _dbContext.SaveChangesAsync();
        return createdAsSuperAdmin;
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

        // Ensure SuperAdmins have AllPermissions in database
        await EnsureSuperAdminPermissions(user.Id);

        // Generate tokens and response
        var authResponse = await CreateAuthenticationResponseAsync(user);

        return Result<AuthenticationResponse>.Success(authResponse);
    }

    public async Task<Result<AuthenticationResponse>> LoginModeratorAsync(string email, string password)
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

        // Check user is Admin or Moderator (Instructor or Preacher)
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(Domain.Constants.Roles.Administrator) &&
            !roles.Contains(Domain.Constants.Roles.Instructor) &&
            !roles.Contains(Domain.Constants.Roles.Preacher))
        {
            return Result<AuthenticationResponse>.Failure(new[] { "You do not have permission to access the admin dashboard." });
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Ensure SuperAdmins have AllPermissions in database
        await EnsureSuperAdminPermissions(user.Id);

        // Generate tokens and response
        var authResponse = await CreateAuthenticationResponseAsync(user);

        return Result<AuthenticationResponse>.Success(authResponse);
    }

    // New: Google sign-in using ID token
    public async Task<Result<AuthenticationResponse>> LoginWithGoogleAsync(string idToken)
    {
        try
        {
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            var androidClientId = _configuration["Authentication:Google:AndroidClientId"];
            var androidWebClientId = _configuration["Authentication:Google:AndroidWebClientId"];

            if (string.IsNullOrWhiteSpace(googleClientId))
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Google ClientId not configured." });
            }

            // Support multiple client IDs (Web, Android, and Android Web Client)
            var validAudiences = new List<string> { googleClientId };
            if (!string.IsNullOrWhiteSpace(androidClientId))
            {
                validAudiences.Add(androidClientId);
            }
            if (!string.IsNullOrWhiteSpace(androidWebClientId))
            {
                validAudiences.Add(androidWebClientId);
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = validAudiences
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
            }
            catch (InvalidJwtException ex)
            {
                // Decode token to see actual audience for debugging
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(idToken);
                    var actualAudience = jwtToken.Audiences.FirstOrDefault();
                    return Result<AuthenticationResponse>.Failure(new[] {
                        $"Invalid Google token: {ex.Message}",
                        $"Expected audiences: {string.Join(", ", validAudiences)}",
                        $"Actual audience in token: {actualAudience}"
                    });
                }
                catch
                {
                    return Result<AuthenticationResponse>.Failure(new[] { $"Invalid Google token: {ex.Message}" });
                }
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
                    Role = UserRole.NonMuslim, // OAuth users default to NonMuslim role
                    ReligionStatus = ReligiousStatus.NonMuslim, // default for OAuth users
                    ImageUrl = imageUrl,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Generate a random password for OAuth users (they won't use it)
                var randomPassword = GenerateRandomPassword();
                var result = await _userManager.CreateAsync(user, randomPassword);
                if (!result.Succeeded)
                {
                    return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
                }

                await _userManager.AddToRoleAsync(user, GetRoleName(user.Role));

                // Create student entity for default role (not SuperAdmin)
                await CreateRoleSpecificEntity(user.Id, user.Role, isSuperAdmin: false);

                // Grant default permissions for the role
                var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(user.Role.ToString());
                await _permissionService.SetUserPermissionsAsync(user.Id.ToString(), rolePermissions, null, $"Auto-granted on Google login for role {user.Role}");
            }

            if (!user.IsActive)
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Your account has been deactivated. Please contact support for assistance." });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Ensure SuperAdmins have AllPermissions in database
            await EnsureSuperAdminPermissions(user.Id);

            // Generate tokens and response
            var authResponse = await CreateAuthenticationResponseAsync(user);

            return Result<AuthenticationResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            // Catch any unexpected errors to prevent 500 errors
            // Include inner exception details for database errors
            var errorMessage = ex.InnerException != null
                ? $"An error occurred during Google authentication: {ex.Message} | Inner: {ex.InnerException.Message}"
                : $"An error occurred during Google authentication: {ex.Message}";

            return Result<AuthenticationResponse>.Failure(new[] { errorMessage });
        }
    }

    // New: Facebook sign-in using access token
    public async Task<Result<AuthenticationResponse>> LoginWithFacebookAsync(string accessToken)
    {
        try
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
            HttpResponseMessage debugResponse;
            try
            {
                debugResponse = await http.GetAsync(debugUri);
            }
            catch (HttpRequestException ex)
            {
                return Result<AuthenticationResponse>.Failure(new[] { $"Failed to connect to Facebook API: {ex.Message}" });
            }
            catch (TaskCanceledException)
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Facebook API request timed out." });
            }

            if (!debugResponse.IsSuccessStatusCode)
            {
                var errorContent = await debugResponse.Content.ReadAsStringAsync();
                return Result<AuthenticationResponse>.Failure(new[] { $"Invalid Facebook token (debug call failed): {errorContent}" });
            }

            JsonDocument debugDoc;
            try
            {
                var debugContent = await debugResponse.Content.ReadAsStringAsync();
                debugDoc = JsonDocument.Parse(debugContent);
            }
            catch (JsonException ex)
            {
                return Result<AuthenticationResponse>.Failure(new[] { $"Failed to parse Facebook debug token response: {ex.Message}" });
            }

            using (debugDoc)
            {
                if (!debugDoc.RootElement.TryGetProperty("data", out var dataEl))
                {
                    return Result<AuthenticationResponse>.Failure(new[] { "Invalid Facebook debug token response format." });
                }

                if (!dataEl.TryGetProperty("is_valid", out var isValidProp))
                {
                    return Result<AuthenticationResponse>.Failure(new[] { "Facebook debug token response missing 'is_valid' field." });
                }

                var isValid = isValidProp.GetBoolean();
                if (!isValid)
                {
                    return Result<AuthenticationResponse>.Failure(new[] { "Invalid Facebook token." });
                }
            }

            // Step 2: fetch user profile
            var fields = "id,name,first_name,last_name,email";
            // email permission might not be granted; handle null email.
            // Using latest Graph API version
            var meUri = $"https://graph.facebook.com/me?fields={fields}&access_token={accessToken}";
            HttpResponseMessage meResp;
            try
            {
                meResp = await http.GetAsync(meUri);
            }
            catch (HttpRequestException ex)
            {
                return Result<AuthenticationResponse>.Failure(new[] { $"Failed to fetch Facebook user profile: {ex.Message}" });
            }
            catch (TaskCanceledException)
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Facebook user profile request timed out." });
            }

            if (!meResp.IsSuccessStatusCode)
            {
                var errorContent = await meResp.Content.ReadAsStringAsync();
                return Result<AuthenticationResponse>.Failure(new[] { $"Unable to fetch Facebook user profile: {errorContent}" });
            }

            JsonDocument meDoc;
            string? email, firstName, lastName, fbId;
            try
            {
                var meContent = await meResp.Content.ReadAsStringAsync();
                meDoc = JsonDocument.Parse(meContent);

                email = meDoc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
                firstName = meDoc.RootElement.TryGetProperty("first_name", out var fnProp) ? fnProp.GetString() : null;
                lastName = meDoc.RootElement.TryGetProperty("last_name", out var lnProp) ? lnProp.GetString() : null;
                fbId = meDoc.RootElement.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                meDoc.Dispose();
            }
            catch (JsonException ex)
            {
                return Result<AuthenticationResponse>.Failure(new[] { $"Failed to parse Facebook user profile: {ex.Message}" });
            }

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
                if (string.IsNullOrWhiteSpace(fbId))
                {
                    return Result<AuthenticationResponse>.Failure(new[] { "Facebook user ID not available." });
                }
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
                    Role = UserRole.NonMuslim, // OAuth users default to NonMuslim role
                    ReligionStatus = ReligiousStatus.NonMuslim, // default for OAuth users
                    ImageUrl = imageUrl,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Generate a random password for OAuth users (they won't use it)
                var randomPassword = GenerateRandomPassword();
                var result = await _userManager.CreateAsync(user, randomPassword);
                if (!result.Succeeded)
                {
                    return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
                }

                await _userManager.AddToRoleAsync(user, GetRoleName(user.Role));

                // Create student entity for default role (not SuperAdmin)
                await CreateRoleSpecificEntity(user.Id, user.Role, isSuperAdmin: false);

                // Grant default permissions for the role
                var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(user.Role.ToString());
                await _permissionService.SetUserPermissionsAsync(user.Id.ToString(), rolePermissions, null, $"Auto-granted on Facebook login for role {user.Role}");
            }

            if (!user.IsActive)
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Your account has been deactivated. Please contact support for assistance." });
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Ensure SuperAdmins have AllPermissions in database
            await EnsureSuperAdminPermissions(user.Id);

            // Generate tokens and response
            var authResponse = await CreateAuthenticationResponseAsync(user);

            return Result<AuthenticationResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            // Catch any unexpected errors to prevent 500 errors
            // Include inner exception details for database errors
            var errorMessage = ex.InnerException != null
                ? $"An error occurred during Facebook authentication: {ex.Message} | Inner: {ex.InnerException.Message}"
                : $"An error occurred during Facebook authentication: {ex.Message}";

            return Result<AuthenticationResponse>.Failure(new[] { errorMessage });
        }
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

        // Check role-based permissions from database (RolePermission table)
        // This allows super admins to update role permissions dynamically
        var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(user.Role.ToString());

        // Check if the role's permissions satisfy the policy requirement
        var hasRolePermission = CheckPermissionForPolicy(policyName, rolePermissions);

        return hasRolePermission;
    }

    private bool CheckPermissionForPolicy(string policyName, Domain.Enums.Permission userPermissions)
    {
        // NOTE: This policyPermissionMap is intentionally hardcoded and different from the removed policyRoleMap.
        // It maps policy names (strings) to Permission enum flags, which is a technical mapping that should remain static.
        // Role-to-permission mappings are now database-driven via RolePermission table.
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

    private async Task EnsureSuperAdminPermissions(Guid userId)
    {
        var admin = await _dbContext.Admins
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (admin?.IsSuperAdmin == true)
        {
            var userPermission = await _dbContext.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (userPermission == null)
            {
                _dbContext.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    Permissions = Permission.AllPermissions,
                    GrantedDate = DateTime.UtcNow,
                    Notes = "SuperAdmin - auto-synced on login"
                });
                await _dbContext.SaveChangesAsync();
            }
            else if (userPermission.Permissions != Permission.AllPermissions)
            {
                userPermission.Permissions = Permission.AllPermissions;
                userPermission.GrantedDate = DateTime.UtcNow;
                userPermission.Notes = "SuperAdmin - auto-synced on login";
                await _dbContext.SaveChangesAsync();
            }
        }
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

    /// <summary>
    /// Generates a cryptographically secure random password for OAuth users.
    /// OAuth users don't use passwords but Identity requires PasswordHash.
    /// </summary>
    private static string GenerateRandomPassword()
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var random = new Random();
        var chars = new char[32];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = validChars[random.Next(validChars.Length)];
        }
        return new string(chars);
    }

    public async Task<Result> LogoutAsync(string tokenId, DateTime tokenExpiration)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                return Result.Failure(new[] { "Invalid token." });
            }

            await _tokenBlacklistService.BlacklistTokenAsync(tokenId, tokenExpiration);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new[] { $"Logout failed: {ex.Message}" });
        }
    }

    public async Task<Result<AuthenticationResponse>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Refresh token is required." });
            }

            // Find user by refresh token
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null)
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Invalid refresh token." });
            }

            // Check if refresh token is expired
            if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Refresh token has expired. Please log in again." });
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Result<AuthenticationResponse>.Failure(new[] { "Your account has been deactivated." });
            }

            // Generate new tokens (token rotation)
            var newJwtToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();
            var refreshTokenExpiryDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);
            var tokenExpirationHours = _configuration.GetValue<int>("JwtSettings:TokenExpirationHours", 24);

            // Update user with new refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var authResponse = new AuthenticationResponse
            {
                UserId = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                ReligiousStatus = user.ReligionStatus,
                Token = newJwtToken,
                ExpiresAt = DateTime.UtcNow.AddHours(tokenExpirationHours),
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresAt = user.RefreshTokenExpiryTime.Value
            };

            return Result<AuthenticationResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            return Result<AuthenticationResponse>.Failure(new[] { $"Token refresh failed: {ex.Message}" });
        }
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task<AuthenticationResponse> CreateAuthenticationResponseAsync(User user)
    {
        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        var tokenExpirationHours = _configuration.GetValue<int>("JwtSettings:TokenExpirationHours", 24);
        var refreshTokenExpiryDays = _configuration.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);

        // Save refresh token to user
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);
        await _userManager.UpdateAsync(user);

        return new AuthenticationResponse
        {
            UserId = user.Id.ToString(),
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            ReligiousStatus = user.ReligionStatus,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(tokenExpirationHours),
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = user.RefreshTokenExpiryTime.Value
        };
    }
}
