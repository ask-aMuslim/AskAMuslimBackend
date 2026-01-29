using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IEventNotificationService
{
    Task SendEventNotificationAsync(
        Guid eventId,
        string eventTitle,
        string? description,
        DateTime startDateTime,
        DateTime endDateTime,
        string? meetingLink,
        string? speakerName,
        UserRole? targetRole,
        CancellationToken cancellationToken = default);
}
