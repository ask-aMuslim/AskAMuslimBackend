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
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(
        IApplicationDbContext context,
        IFirebaseMessagingService firebaseMessaging,
        ILogger<SendNotificationCommandHandler> logger)
    {
        _context = context;
        _firebaseMessaging = firebaseMessaging;
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
                .Select(u => new { u.FcmToken })
                .FirstOrDefaultAsync(cancellationToken);

            if (user?.FcmToken != null)
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification to user {UserId}", request.UserId);
            // Don't fail the command if push notification fails
        }

        return Result<Guid>.Success(notification.Id);
    }
}

