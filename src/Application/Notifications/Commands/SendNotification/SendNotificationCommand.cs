using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.Notifications.Commands.SendNotification;

public record SendNotificationCommand : IRequest<Result<Guid>>
{
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public string? ImageUrl { get; init; }
    public Dictionary<string, string>? CustomData { get; init; }
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IFirebaseMessagingService _firebaseMessaging;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        IApplicationDbContext context,
        IFirebaseMessagingService firebaseMessaging,
        IEmailService emailService,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _context = context;
        _firebaseMessaging = firebaseMessaging;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            ActionUrl = request.ActionUrl,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // Send push notification via Firebase
        try
        {
            var user = await _context.Users
                .Where(u => u.Id == request.UserId)
                .Select(u => new { u.FcmToken, u.Email, u.FirstName })
                .FirstOrDefaultAsync(cancellationToken);

            if (user != null)
            {
                // Send mobile push notification if user has FCM token
                if (user.FcmToken != null)
                {
                    var pushNotification = new PushNotificationRequest
                    {
                        Title = request.Title,
                        Body = request.Message,
                        ActionUrl = request.ActionUrl,
                        ImageUrl = request.ImageUrl,
                        CustomData = request.CustomData
                    };

                    var result = await _firebaseMessaging.SendToTokenAsync(user.FcmToken, pushNotification, cancellationToken);

                    if (result.InvalidTokens.Any())
                    {
                        // Clean up invalid token
                        var userEntity = await _context.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
                        if (userEntity != null)
                        {
                            userEntity.FcmToken = null;
                            await _context.SaveChangesAsync(cancellationToken);
                        }
                    }

                    _logger.LogInformation(
                        "Push notification sent to user {UserId}. Success: {Success}",
                        request.UserId,
                        result.IsSuccess);
                }
                else
                {
                    _logger.LogInformation("User {UserId} has no FCM token. Skipping push notification.", request.UserId);
                }

                // Send email notification if user has email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        var emailBody = GenerateNotificationEmailHtml(user.FirstName, request.Title, request.Message, request.ActionUrl);
                        await _emailService.SendEmailAsync(user.Email, request.Title, emailBody, isHtml: true);
                        _logger.LogInformation("Email notification sent to user {UserId}", request.UserId);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send email notification to user {UserId}", request.UserId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notifications to user {UserId}", request.UserId);
            // Don't fail the command if notifications fail
        }

        return Result<Guid>.Success(notification.Id);
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

