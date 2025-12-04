using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCoursesWithPagination;

public record GetCoursesWithPaginationQuery : IRequest<Result<PaginatedList<CourseBriefDto>>>, IPaginationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetCoursesWithPaginationQueryHandler : IRequestHandler<GetCoursesWithPaginationQuery, Result<PaginatedList<CourseBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCoursesWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<CourseBriefDto>>> Handle(GetCoursesWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var paginatedList = await _context.Courses
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Title)
            .ProjectTo<CourseBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
            
        return Result<PaginatedList<CourseBriefDto>>.Success(paginatedList);
    }
}
