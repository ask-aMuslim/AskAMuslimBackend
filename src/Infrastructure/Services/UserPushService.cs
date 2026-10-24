using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services;

public class UserPushService : IUserPushService
{
    private readonly ApplicationDbContext _context;
    private readonly IFirebaseMessagingService _firebaseMessaging;
    private readonly ILogger<UserPushService> _logger;

    public UserPushService(ApplicationDbContext context, IFirebaseMessagingService firebaseMessaging, ILogger<UserPushService> logger)
    {
        _context = context;
        _firebaseMessaging = firebaseMessaging;
        _logger = logger;
    }

    public async Task<int> SendToUserAsync(Guid userId, PushNotificationRequest notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokens = await _context.UserDeviceTokens
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .ToListAsync(cancellationToken);

            // Fall back to the legacy single token for users who registered before multi-device support.
            var legacyToken = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.FcmToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(legacyToken) && !tokens.Contains(legacyToken))
            {
                tokens.Add(legacyToken);
            }

            if (tokens.Count == 0)
            {
                _logger.LogInformation("User {UserId} has no registered devices. Skipping push notification.", userId);
                return 0;
            }

            var result = await _firebaseMessaging.SendToTokensAsync(tokens, notification, cancellationToken);
            if (result == null)
            {
                return 0;
            }

            if (result.InvalidTokens.Count > 0)
            {
                await RemoveInvalidTokensAsync(userId, result.InvalidTokens, cancellationToken);
            }

            _logger.LogInformation(
                "Push notification sent to user {UserId}: {SuccessCount} delivered, {FailureCount} failed.",
                userId, result.SuccessCount, result.FailureCount);

            return result.SuccessCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to user {UserId}", userId);
            return 0;
        }
    }

    private async Task RemoveInvalidTokensAsync(Guid userId, List<string> invalidTokens, CancellationToken cancellationToken)
    {
        var deviceTokens = await _context.UserDeviceTokens
            .Where(t => invalidTokens.Contains(t.Token))
            .ToListAsync(cancellationToken);
        _context.UserDeviceTokens.RemoveRange(deviceTokens);

        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user?.FcmToken != null && invalidTokens.Contains(user.FcmToken))
        {
            user.FcmToken = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Removed {Count} invalid device token(s) for user {UserId}", invalidTokens.Count, userId);
    }
}
