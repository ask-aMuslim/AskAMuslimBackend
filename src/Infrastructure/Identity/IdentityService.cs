using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace AskAMuslimBackend.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<Result<AuthenticationResponse>> RegisterUserAsync(
        string email, 
        string password, 
        string firstName, 
        string lastName, 
        UserRole role)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "User with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            EmailConfirmed = true // Auto-confirm for now
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            return Result<AuthenticationResponse>.Failure(result.Errors.Select(e => e.Description));
        }

        // Add role to user
        var roleName = role.ToString();
        await _userManager.AddToRoleAsync(user, roleName);

        // Generate token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName!,
            LastName = user.LastName!,
            Role = user.Role,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthenticationResponse>.Success(authResponse);
    }

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

        // Generate token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Role = user.Role,
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

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = UserRole.Student, // default role, adjust as needed
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Result<AuthenticationResponse>.Failure(createResult.Errors.Select(e => e.Description));
            }
            await _userManager.AddToRoleAsync(user, user.Role.ToString());
        }

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Role = user.Role,
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

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = UserRole.Student,
                EmailConfirmed = true
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                return Result<AuthenticationResponse>.Failure(createResult.Errors.Select(e => e.Description));
            }
            await _userManager.AddToRoleAsync(user, user.Role.ToString());
        }

        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        var authResponse = new AuthenticationResponse
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Role = user.Role,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthenticationResponse>.Success(authResponse);
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured");
        var issuer = jwtSettings["Issuer"] ?? "AskAMuslimBackend";
        var audience = jwtSettings["Audience"] ?? "AskAMuslimBackend";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
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
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }
}
