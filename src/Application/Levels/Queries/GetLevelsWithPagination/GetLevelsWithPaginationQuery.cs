using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelsWithPagination;

public record GetLevelsWithPaginationQuery : IRequest<Result<PaginatedList<LevelBriefDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
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
        var query = _context.Levels.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(l => 
                l.Title.Contains(request.SearchTerm) ||
                (l.Description != null && l.Description.Contains(request.SearchTerm)));
        }

        var paginatedList = await query
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Title)
            .ProjectTo<LevelBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
            
        return Result<PaginatedList<LevelBriefDto>>.Success(paginatedList);
    }
}
