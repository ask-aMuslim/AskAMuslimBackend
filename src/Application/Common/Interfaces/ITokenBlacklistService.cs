namespace AskAMuslimBackend.Application.Common.Interfaces;

/// <summary>
/// Service for managing invalidated JWT tokens (blacklist).
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist until its expiration time.
    /// </summary>
    /// <param name="tokenId">The JTI (JWT ID) claim of the token.</param>
    /// <param name="expirationTime">When the token expires naturally.</param>
    Task BlacklistTokenAsync(string tokenId, DateTime expirationTime);

    /// <summary>
    /// Checks if a token is blacklisted (invalidated).
    /// </summary>
    /// <param name="tokenId">The JTI (JWT ID) claim to check.</param>
    /// <returns>True if the token is blacklisted, false otherwise.</returns>
    Task<bool> IsTokenBlacklistedAsync(string tokenId);
}
