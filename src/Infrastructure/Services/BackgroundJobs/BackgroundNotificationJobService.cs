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
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationJobExecutor> _logger;
    private const int BatchSize = 200;

    public NotificationJobExecutor(
        IApplicationDbContext context,
        IFirebaseMessagingService firebaseMessaging,
        IEmailService emailService,
        ILogger<NotificationJobExecutor> logger)
    {
        _context = context;
        _firebaseMessaging = firebaseMessaging;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendToSpecificUsersAsync(
        IEnumerable<Guid> userIds, 
        PushNotificationRequest notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bulk notification job for {Count} specific users", userIds.Count());

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id) && u.IsActive)
            .Select(u => new { u.FcmToken, u.Email, u.FirstName })
            .ToListAsync(cancellationToken);

        // Send push notifications
        var tokensToSend = users.Where(u => u.FcmToken != null).Select(u => u.FcmToken!).ToList();
        if (tokensToSend.Any())
        {
            await ProcessNotificationBatchAsync(tokensToSend, notification, cancellationToken);
        }

        // Send email notifications
        await SendEmailNotificationsAsync(users, notification, cancellationToken);
        
        _logger.LogInformation("Completed bulk notification job. Processed {Count} users", users.Count);
    }

    public async Task SendToRoleAsync(
        UserRole role, 
        PushNotificationRequest notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting role-based notification job for role: {Role}", role);

        var users = await _context.Users
            .Where(u => u.Role == role && u.IsActive)
            .Select(u => new { u.FcmToken, u.Email, u.FirstName })
            .ToListAsync(cancellationToken);

        // Send push notifications
        var tokensToSend = users.Where(u => u.FcmToken != null).Select(u => u.FcmToken!).ToList();
        if (tokensToSend.Any())
        {
            await ProcessNotificationBatchAsync(tokensToSend, notification, cancellationToken);
        }

        // Send email notifications
        await SendEmailNotificationsAsync(users, notification, cancellationToken);
        
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
                .Where(u => u.IsActive)
                .OrderBy(u => u.Id)
                .Skip(skip)
                .Take(pageSize)
                .Select(u => new { u.FcmToken, u.Email, u.FirstName })
                .ToListAsync(cancellationToken);

            if (users.Count == 0)
            {
                hasMore = false;
                continue;
            }

            // Send push notifications
            var tokensToSend = users.Where(u => u.FcmToken != null).Select(u => u.FcmToken!).ToList();
            if (tokensToSend.Any())
            {
                await ProcessNotificationBatchAsync(tokensToSend, notification, cancellationToken);
            }

            // Send email notifications
            await SendEmailNotificationsAsync(users, notification, cancellationToken);
            
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

    private async Task SendEmailNotificationsAsync<T>(
        IEnumerable<T> users,
        PushNotificationRequest notification,
        CancellationToken cancellationToken)
    {
        var usersWithEmail = users
            .Select(u =>
            {
                var emailProp = typeof(T).GetProperty("Email")?.GetValue(u) as string;
                var firstNameProp = typeof(T).GetProperty("FirstName")?.GetValue(u) as string;
                return new { Email = emailProp, FirstName = firstNameProp };
            })
            .Where(u => !string.IsNullOrEmpty(u.Email))
            .ToList();

        if (!usersWithEmail.Any())
        {
            return;
        }

        try
        {
            // Send emails in batches to avoid overwhelming the email service
            const int emailBatchSize = 50;
            var emailBatches = usersWithEmail.Chunk(emailBatchSize);

            foreach (var batch in emailBatches)
            {
                var emailTasks = batch.Select(user =>
                    SendNotificationEmailAsync(user.Email!, user.FirstName ?? "User", notification));

                await Task.WhenAll(emailTasks);
            }

            _logger.LogInformation("Sent {Count} email notifications", usersWithEmail.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email notifications batch");
        }
    }

    private async Task SendNotificationEmailAsync(
        string email,
        string userName,
        PushNotificationRequest notification)
    {
        try
        {
            var emailBody = GenerateNotificationEmailHtml(userName, notification.Title, notification.Body, notification.ActionUrl);
            await _emailService.SendEmailAsync(email, notification.Title, emailBody, isHtml: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
        }
    }

    private static string GenerateNotificationEmailHtml(string userName, string title, string message, string? actionUrl)
    {
        var actionSection = !string.IsNullOrEmpty(actionUrl)
            ? $@"<div class=""action-section"">
                    <a href=""{actionUrl}"" class=""btn"">View Details</a>
                 </div>"
            : "";

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Notification</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7fa;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #FF9800 0%, #F57C00 100%);
            padding: 40px 30px;
            text-align: center;
            color: #ffffff;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .greeting {{
            font-size: 18px;
            margin-bottom: 20px;
            color: #333333;
        }}
        .notification-title {{
            font-size: 24px;
            font-weight: 700;
            color: #FF9800;
            margin: 20px 0;
        }}
        .message {{
            font-size: 16px;
            line-height: 1.8;
            color: #555555;
            background-color: #f8f9fa;
            padding: 20px;
            border-left: 4px solid #FF9800;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .action-section {{
            text-align: center;
            margin: 30px 0;
        }}
        .btn {{
            display: inline-block;
            background: linear-gradient(135deg, #FF9800, #F57C00);
            color: white;
            padding: 15px 40px;
            text-decoration: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            box-shadow: 0 4px 12px rgba(255, 152, 0, 0.3);
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer-text {{
            font-size: 13px;
            color: #6c757d;
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>🔔 New Notification</h1>
        </div>
        
        <div class=""content"">
            <div class=""greeting"">
                Hello {userName},
            </div>
            
            <div class=""notification-title"">{title}</div>
            
            <div class=""message"">
                {message}
            </div>
            
            {actionSection}
        </div>
        
        <div class=""footer"">
            <p class=""footer-text"">
                © 2026 Ask A Muslim. All rights reserved.
            </p>
        </div>
    </div>
</body>
</html>";
    }
}
