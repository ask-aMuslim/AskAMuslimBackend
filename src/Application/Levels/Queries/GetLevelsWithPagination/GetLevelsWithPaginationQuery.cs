using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelsWithPagination;

public record GetLevelsWithPaginationQuery : IRequest<PaginatedList<LevelBriefDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetLevelsWithPaginationQueryHandler : IRequestHandler<GetLevelsWithPaginationQuery, PaginatedList<LevelBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLevelsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<LevelBriefDto>> Handle(GetLevelsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        return await _context.Levels
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Title)
            .ProjectTo<LevelBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}
