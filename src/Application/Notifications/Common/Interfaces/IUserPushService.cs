using AskAMuslimBackend.Application.Notifications.Common.Models;

namespace AskAMuslimBackend.Application.Notifications.Common.Interfaces;

/// <summary>
/// Sends a push notification to every registered device of a user and cleans up tokens Firebase rejects.
/// Never throws for delivery failures; they are logged.
/// </summary>
public interface IUserPushService
{
    /// <returns>The number of devices the push was delivered to.</returns>
    Task<int> SendToUserAsync(Guid userId, PushNotificationRequest notification, CancellationToken cancellationToken = default);
}
