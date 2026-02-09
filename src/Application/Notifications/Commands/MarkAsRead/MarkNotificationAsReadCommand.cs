using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Exceptions;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Notifications.Commands.MarkAsRead;

public record MarkNotificationAsReadCommand : IRequest<Result>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public Guid NotificationId { get; init; }
}

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public MarkNotificationAsReadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _context.Notifications
            .FindAsync(new object[] { request.NotificationId }, cancellationToken);

        if (notification == null)
        {
            return Result.Failure(new[] { $"Notification with ID {request.NotificationId} not found" });
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}
