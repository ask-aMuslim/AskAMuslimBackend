using AskAMuslimBackend.Application.Notifications.Common.Models;

namespace AskAMuslimBackend.Application.Notifications.Common.Interfaces;

public interface IFirebaseMessagingService
{
    /// <summary>
    /// Sends a push notification to a single device token
    /// </summary>
    Task<PushNotificationResult> SendToTokenAsync(string fcmToken, PushNotificationRequest notification, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a push notification to multiple device tokens (batch)
    /// </summary>
    Task<PushNotificationResult> SendToTokensAsync(IEnumerable<string> fcmTokens, PushNotificationRequest notification, CancellationToken cancellationToken = default);
}
