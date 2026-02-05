using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Events.Queries.GetEventsWithPagination;

public record GetEventsWithPaginationQuery : IRequest<Result<PaginatedList<EventDto>>>, IPaginationQuery
{
    public bool? IsPublished { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetEventsWithPaginationQueryHandler : IRequestHandler<GetEventsWithPaginationQuery, Result<PaginatedList<EventDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<EventDto>>> Handle(GetEventsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Events
            .Include(e => e.Registrations)
            .AsNoTracking()
            .AsQueryable();

        if (request.IsPublished.HasValue)
            query = query.Where(e => e.IsPublished == request.IsPublished.Value);
        if (request.From.HasValue)
            query = query.Where(e => e.StartDateTime >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(e => e.EndDateTime <= request.To.Value);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = query.Where(e => 
                e.Title.Contains(request.SearchTerm) ||
                (e.Description != null && e.Description.Contains(request.SearchTerm)) ||
                (e.SpeakerName != null && e.SpeakerName.Contains(request.SearchTerm)));

        var events = await query
            .OrderBy(e => e.StartDateTime)
            .ProjectTo<EventDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
            
        return Result<PaginatedList<EventDto>>.Success(events);
    }
}
