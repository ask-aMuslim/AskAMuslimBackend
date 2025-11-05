using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<Result> DeleteUserAsync(string userId);

    Task<Result<AuthenticationResponse>> RegisterUserAsync(
        string email, 
        string password, 
        string firstName, 
        string lastName, 
        UserRole role);

    Task<Result<AuthenticationResponse>> LoginUserAsync(string email, string password);
}
