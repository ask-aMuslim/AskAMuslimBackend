using AskAMuslimBackend.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AskAMuslimBackend.Infrastructure.Services;

/// <summary>
/// In-memory implementation of token blacklist service.
/// For production with multiple servers, consider using Redis or database.
/// </summary>
public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;
    private const string BlacklistPrefix = "blacklisted_token_";

    public TokenBlacklistService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task BlacklistTokenAsync(string tokenId, DateTime expirationTime)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            throw new ArgumentException("Token ID cannot be null or empty", nameof(tokenId));
        }

        var cacheKey = GetCacheKey(tokenId);
        var cacheExpiration = expirationTime > DateTime.UtcNow
            ? expirationTime
            : DateTime.UtcNow.AddMinutes(5); // Minimum 5 minutes for already expired tokens

        _cache.Set(cacheKey, true, cacheExpiration);

        return Task.CompletedTask;
    }

    public Task<bool> IsTokenBlacklistedAsync(string tokenId)
    {
        if (string.IsNullOrWhiteSpace(tokenId))
        {
            return Task.FromResult(false);
        }

        var cacheKey = GetCacheKey(tokenId);
        var isBlacklisted = _cache.TryGetValue(cacheKey, out _);

        return Task.FromResult(isBlacklisted);
    }

    private static string GetCacheKey(string tokenId) => $"{BlacklistPrefix}{tokenId}";
}
