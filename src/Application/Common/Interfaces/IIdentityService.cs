using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    //Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    Task<Result<AuthenticationResponse>> RegisterUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        UserRole role,
        ReligiousStatus religiousStatus = ReligiousStatus.NonMuslim,
        IFormFile? image = null,
        CancellationToken cancellationToken = default,
        bool allowAdminCreation = false);

    Task<Result<AuthenticationResponse>> LoginUserAsync(string email, string password);

    Task<Result<AuthenticationResponse>> LoginModeratorAsync(string email, string password);

    // New: Login or register using Google ID token
    Task<Result<AuthenticationResponse>> LoginWithGoogleAsync(string idToken);

    // New: Login or register using Facebook access token
    Task<Result<AuthenticationResponse>> LoginWithFacebookAsync(string accessToken);

    /// <summary>
    /// Logs out a user by blacklisting their current JWT token.
    /// </summary>
    /// <param name="tokenId">The JTI (JWT ID) claim from the user's token.</param>
    /// <param name="tokenExpiration">The expiration time of the token.</param>
    Task<Result> LogoutAsync(string tokenId, DateTime tokenExpiration);
}
