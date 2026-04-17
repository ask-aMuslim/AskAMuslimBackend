using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Events.Queries.GetEventsWithPagination;

public record GetEventsWithPaginationQuery : IRequest<Result<PaginatedList<EventDto>>>, IPaginationQuery
{
    public bool? IsPublished { get; init; }
    public EventStatus? EventStatus { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetEventsWithPaginationQueryHandler : IRequestHandler<GetEventsWithPaginationQuery, Result<PaginatedList<EventDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetEventsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<EventDto>>> Handle(GetEventsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        var query = _context.Events
            .Include(e => e.Registrations)
            .AsNoTracking()
            .AsQueryable();

        if (request.IsPublished.HasValue)
            query = query.Where(e => e.IsPublished == request.IsPublished.Value);

        if (request.EventStatus.HasValue)
        {
            query = request.EventStatus.Value switch
            {
                EventStatus.Upcoming => query.Where(e => e.StartDateTime > now),
                EventStatus.Live => query.Where(e => e.StartDateTime <= now && e.EndDateTime >= now),
                EventStatus.Finished => query.Where(e => e.EndDateTime < now),
                _ => query
            };
        }

        if (request.From.HasValue)
            query = query.Where(e => e.StartDateTime >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(e => e.EndDateTime <= request.To.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(e =>
                e.Title.Contains(request.SearchTerm) ||
                (e.Description != null && e.Description.Contains(request.SearchTerm)) ||
                (e.SpeakerName != null && e.SpeakerName.Contains(request.SearchTerm)));

        var pagedEventData = await query
            .OrderBy(e => e.StartDateTime)
            .Select(s => new
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                SpeakerName = s.SpeakerName,
                ImageUrl = s.ImageUrl,
                StartDateTime = s.StartDateTime,
                EndDateTime = s.EndDateTime,
                MeetingLink = s.MeetingLink,
                IsPublished = s.IsPublished,
                TotalRegistrations = s.Registrations.Count
            })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        var mappedItems = pagedEventData.Items
            .Select(s => new EventDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                SpeakerName = s.SpeakerName,
                ImageUrl = s.ImageUrl,
                StartDateTime = s.StartDateTime,
                EndDateTime = s.EndDateTime,
                Status = DetermineEventStatus(s.StartDateTime, s.EndDateTime, now),
                MeetingLink = s.MeetingLink,
                IsPublished = s.IsPublished,
                TotalRegistrations = s.TotalRegistrations
            })
            .ToList();

        var events = new PaginatedList<EventDto>(
            mappedItems,
            pagedEventData.TotalCount,
            pagedEventData.PageNumber,
            request.PageSize ?? 10);

        return Result<PaginatedList<EventDto>>.Success(events);
    }

    private static EventStatus DetermineEventStatus(DateTime startDateTime, DateTime endDateTime, DateTime now)
        => now < startDateTime ? EventStatus.Upcoming : now <= endDateTime ? EventStatus.Live : EventStatus.Finished;
}
