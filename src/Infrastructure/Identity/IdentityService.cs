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

namespace AskAMuslimBackend.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly IConfiguration _configuration;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _configuration = configuration;
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
