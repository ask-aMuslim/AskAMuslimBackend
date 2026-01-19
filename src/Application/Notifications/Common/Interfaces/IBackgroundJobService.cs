using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Notifications.Common.Interfaces;

public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a bulk notification job for specific user IDs
    /// </summary>
    string EnqueueBulkNotification(IEnumerable<Guid> userIds, PushNotificationRequest notification);
    
    /// <summary>
    /// Enqueues a notification job for users with a specific role
    /// </summary>
    string EnqueueRoleBasedNotification(UserRole role, PushNotificationRequest notification);
    
    /// <summary>
    /// Enqueues a notification job for all users
    /// </summary>
    string EnqueueBroadcastNotification(PushNotificationRequest notification);
}
