using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Events.Queries.GetEventsWithPagination;

public record GetEventsWithPaginationQuery : IRequest<PaginatedList<EventDto>>, IPaginationQuery
{
    public bool? IsPublished { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetEventsWithPaginationQueryHandler : IRequestHandler<GetEventsWithPaginationQuery, PaginatedList<EventDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEventsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<EventDto>> Handle(GetEventsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Events.AsNoTracking().AsQueryable();

        if (request.IsPublished.HasValue)
            query = query.Where(e => e.IsPublished == request.IsPublished.Value);
        if (request.From.HasValue)
            query = query.Where(e => e.StartDateTime >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(e => e.EndDateTime <= request.To.Value);

        return await query
            .OrderBy(e => e.StartDateTime)
            .ProjectTo<EventDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
