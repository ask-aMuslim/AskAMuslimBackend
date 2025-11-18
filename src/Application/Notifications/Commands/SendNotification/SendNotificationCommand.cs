using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Notifications.Commands.SendNotification;

public record SendNotificationCommand : IRequest<Guid>
{
    public Guid UserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public SendNotificationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = request.UserId,
            Title = request.Title,
            Message = request.Message,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // TODO: Integrate with push notification service (Firebase, OneSignal, etc.)

        return notification.Id;
    }
}
