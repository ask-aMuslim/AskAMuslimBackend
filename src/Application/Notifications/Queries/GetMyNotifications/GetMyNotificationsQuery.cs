using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.Notifications.Queries.GetUserNotifications;

namespace AskAMuslimBackend.Application.Notifications.Queries.GetMyNotifications;

/// <summary>
/// The current user's in-app notifications (any signed-in user, no permission required).
/// </summary>
public record GetMyNotificationsQuery : IRequest<Result<PaginatedList<NotificationDto>>>, IPaginationQuery
{
    // Nullable so it stays optional when bound with [AsParameters].
    public bool? UnreadOnly { get; init; }
    public string? NotificationType { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
}

public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, Result<PaginatedList<NotificationDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMyNotificationsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id) || !Guid.TryParse(_user.Id, out var userId))
        {
            return Result<PaginatedList<NotificationDto>>.Failure(new[] { "Unauthorized" });
        }

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (request.UnreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrWhiteSpace(request.NotificationType))
        {
            query = query.Where(n => n.NotificationType == request.NotificationType);
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
                ActionUrl = n.ActionUrl,
                NotificationType = n.NotificationType,
                CreatedAt = n.Created
            })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 20, cancellationToken);

        return Result<PaginatedList<NotificationDto>>.Success(notifications);
    }
}

public class GetMyNotificationsQueryValidator : PaginationQueryValidator<GetMyNotificationsQuery>
{
}
