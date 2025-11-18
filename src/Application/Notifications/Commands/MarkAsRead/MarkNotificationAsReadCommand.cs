using AskAMuslimBackend.Application.Common.Exceptions;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Notifications.Commands.MarkAsRead;

public record MarkNotificationAsReadCommand : IRequest
{
    public Guid NotificationId { get; init; }
}

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkNotificationAsReadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FindAsync(new object[] { request.NotificationId }, cancellationToken);

        if (notification == null)
        {
            throw new NotFoundException(nameof(Notification), request.NotificationId.ToString());
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
