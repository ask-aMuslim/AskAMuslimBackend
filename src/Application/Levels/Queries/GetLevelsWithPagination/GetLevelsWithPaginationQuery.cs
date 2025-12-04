using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelsWithPagination;

public record GetLevelsWithPaginationQuery : IRequest<Result<PaginatedList<LevelBriefDto>>>, IPaginationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetLevelsWithPaginationQueryHandler : IRequestHandler<GetLevelsWithPaginationQuery, Result<PaginatedList<LevelBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLevelsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<LevelBriefDto>>> Handle(GetLevelsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var paginatedList = await _context.Levels
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Title)
            .ProjectTo<LevelBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
            
        return Result<PaginatedList<LevelBriefDto>>.Success(paginatedList);
    }
}
