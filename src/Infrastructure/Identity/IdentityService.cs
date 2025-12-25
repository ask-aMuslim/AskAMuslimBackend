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
using Microsoft.AspNetCore.Identity;
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

    public IdentityService(
        ApplicationDbContext dbContext,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
        ReligiousStatus religiousStatus = ReligiousStatus.BornMuslim)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "User with this email already exists." });
        }

        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            ReligionStatus = religiousStatus,
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
        await _userManager.AddToRoleAsync(user, role.ToString());

        // Create role-specific entity
        await CreateRoleSpecificEntity(user.Id, role);

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
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
            }

            await _userManager.AddToRoleAsync(user, user.Role.ToString());

            // Create student entity for default role
            await CreateRoleSpecificEntity(user.Id, user.Role);
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
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
            }

            await _userManager.AddToRoleAsync(user, user.Role.ToString());

            // Create student entity for default role
            await CreateRoleSpecificEntity(user.Id, user.Role);
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
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString()),
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

        if (user == null)
        {
            return false;
        }

        // For now, we'll do basic policy checking
        // You can extend this to handle specific policies
        return user.IsActive;
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
}
