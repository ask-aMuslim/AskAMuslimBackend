using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Tags.Queries.GetTagsWithPagination;

public record GetTagsWithPaginationQuery : IRequest<Result<PaginatedList<TagBriefDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
}

public class GetTagsWithPaginationQueryHandler : IRequestHandler<GetTagsWithPaginationQuery, Result<PaginatedList<TagBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTagsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<TagBriefDto>>> Handle(GetTagsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var paginatedList = await _context.Tags
            .OrderBy(c => c.Title)
            .ProjectTo<TagBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
            
        return Result<PaginatedList<TagBriefDto>>.Success(paginatedList);
    }
}
