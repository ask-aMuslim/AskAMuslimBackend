using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;

public class BackgroundNotificationJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<BackgroundNotificationJobService> _logger;

    public BackgroundNotificationJobService(
        IBackgroundJobClient backgroundJobClient,
        ILogger<BackgroundNotificationJobService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public string EnqueueBulkNotification(IEnumerable<Guid> userIds, PushNotificationRequest notification)
    {
        _logger.LogInformation("Enqueueing bulk notification for {Count} users", userIds.Count());
        
        return _backgroundJobClient.Enqueue<NotificationJobExecutor>(
            executor => executor.SendToSpecificUsersAsync(userIds, notification, CancellationToken.None));
    }

    public string EnqueueRoleBasedNotification(UserRole role, PushNotificationRequest notification)
    {
        _logger.LogInformation("Enqueueing role-based notification for role: {Role}", role);
        
        return _backgroundJobClient.Enqueue<NotificationJobExecutor>(
            executor => executor.SendToRoleAsync(role, notification, CancellationToken.None));
    }

    public string EnqueueBroadcastNotification(PushNotificationRequest notification)
    {
        _logger.LogInformation("Enqueueing broadcast notification to all users");
        
        return _backgroundJobClient.Enqueue<NotificationJobExecutor>(
            executor => executor.SendToAllUsersAsync(notification, CancellationToken.None));
    }
}

public class NotificationJobExecutor
{
    private readonly IApplicationDbContext _context;
    private readonly IFirebaseMessagingService _firebaseMessaging;
    private readonly ILogger<NotificationJobExecutor> _logger;
    private const int BatchSize = 200;

    public NotificationJobExecutor(
        IApplicationDbContext context,
        IFirebaseMessagingService firebaseMessaging,
        ILogger<NotificationJobExecutor> logger)
    {
        _context = context;
        _firebaseMessaging = firebaseMessaging;
        _logger = logger;
    }

    public async Task SendToSpecificUsersAsync(
        IEnumerable<Guid> userIds, 
        PushNotificationRequest notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bulk notification job for {Count} specific users", userIds.Count());

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id) && u.FcmToken != null && u.IsActive)
            .Select(u => new { u.Id, u.FcmToken })
            .ToListAsync(cancellationToken);

        await ProcessNotificationBatchAsync(users.Select(u => u.FcmToken!), notification, cancellationToken);
        
        _logger.LogInformation("Completed bulk notification job. Processed {Count} users", users.Count);
    }

    public async Task SendToRoleAsync(
        UserRole role, 
        PushNotificationRequest notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting role-based notification job for role: {Role}", role);

        var users = await _context.Users
            .Where(u => u.Role == role && u.FcmToken != null && u.IsActive)
            .Select(u => new { u.Id, u.FcmToken })
            .ToListAsync(cancellationToken);

        await ProcessNotificationBatchAsync(users.Select(u => u.FcmToken!), notification, cancellationToken);
        
        _logger.LogInformation("Completed role-based notification job. Processed {Count} users with role {Role}", users.Count, role);
    }

    public async Task SendToAllUsersAsync(
        PushNotificationRequest notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting broadcast notification job for all users");

        var totalCount = 0;
        var hasMore = true;
        var skip = 0;
        const int pageSize = 1000;

        while (hasMore)
        {
            var users = await _context.Users
                .Where(u => u.FcmToken != null && u.IsActive)
                .OrderBy(u => u.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(u => u.FcmToken!)
                .ToListAsync(cancellationToken);

            if (users.Count == 0)
            {
                hasMore = false;
                continue;
            }

            await ProcessNotificationBatchAsync(users, notification, cancellationToken);
            
            totalCount += users.Count;
            skip += pageSize;
            hasMore = users.Count == pageSize;
            
            _logger.LogInformation("Processed {Count} users. Total so far: {Total}", users.Count, totalCount);
        }

        _logger.LogInformation("Completed broadcast notification job. Total users processed: {Total}", totalCount);
    }

    private async Task ProcessNotificationBatchAsync(
        IEnumerable<string> fcmTokens, 
        PushNotificationRequest notification, 
        CancellationToken cancellationToken)
    {
        var tokens = fcmTokens.ToList();
        if (!tokens.Any())
        {
            return;
        }

        // Process in chunks of BatchSize (200)
        var chunks = tokens.Chunk(BatchSize);
        
        foreach (var chunk in chunks)
        {
            try
            {
                var result = await _firebaseMessaging.SendToTokensAsync(chunk, notification, cancellationToken);

                if (result.InvalidTokens.Any())
                {
                    // Remove invalid tokens from the database
                    await CleanupInvalidTokensAsync(result.InvalidTokens, cancellationToken);
                }

                _logger.LogInformation(
                    "Batch sent: {Success} successful, {Failure} failed, {Invalid} invalid tokens", 
                    result.SuccessCount, 
                    result.FailureCount, 
                    result.InvalidTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification batch of {Count} tokens", chunk.Length);
            }
        }
    }

    private async Task CleanupInvalidTokensAsync(List<string> invalidTokens, CancellationToken cancellationToken)
    {
        if (!invalidTokens.Any())
        {
            return;
        }

        try
        {
            var users = await _context.Users
                .Where(u => invalidTokens.Contains(u.FcmToken!))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.FcmToken = null;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} invalid FCM tokens", invalidTokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up invalid FCM tokens");
        }
    }
}
