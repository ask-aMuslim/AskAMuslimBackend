using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.Notifications.Queries.GetUserNotifications;

public record GetUserNotificationsQuery : IRequest<List<NotificationDto>>
{
    public Guid UserId { get; init; }
    public bool UnreadOnly { get; init; } = false;
}

public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserNotificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == request.UserId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.Created)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.Created
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }
}
