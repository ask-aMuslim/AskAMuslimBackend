using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Notifications.Commands.MarkMyNotificationsAsRead;

/// <summary>Marks one of the current user's notifications as read.</summary>
public record MarkMyNotificationAsReadCommand(Guid NotificationId) : IRequest<Result>;

/// <summary>Marks all of the current user's notifications as read.</summary>
public record MarkAllMyNotificationsAsReadCommand : IRequest<Result>;

public record GetMyUnreadNotificationsCountQuery : IRequest<Result<int>>;

public class MarkMyNotificationAsReadCommandHandler : IRequestHandler<MarkMyNotificationAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public MarkMyNotificationAsReadCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(MarkMyNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id) || !Guid.TryParse(_user.Id, out var userId))
        {
            return Result.Failure(new[] { "Unauthorized" });
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, cancellationToken);

        if (notification == null)
        {
            return Result.Failure(new[] { "Notification not found" });
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

public class MarkAllMyNotificationsAsReadCommandHandler : IRequestHandler<MarkAllMyNotificationsAsReadCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public MarkAllMyNotificationsAsReadCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(MarkAllMyNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id) || !Guid.TryParse(_user.Id, out var userId))
        {
            return Result.Failure(new[] { "Unauthorized" });
        }

        var readAt = _timeProvider.GetUtcNow().UtcDateTime;
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = readAt;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class GetMyUnreadNotificationsCountQueryHandler : IRequestHandler<GetMyUnreadNotificationsCountQuery, Result<int>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMyUnreadNotificationsCountQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<int>> Handle(GetMyUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id) || !Guid.TryParse(_user.Id, out var userId))
        {
            return Result<int>.Failure(new[] { "Unauthorized" });
        }

        var count = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        return Result<int>.Success(count);
    }
}
