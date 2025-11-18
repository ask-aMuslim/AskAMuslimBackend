using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Lessons.Queries.GetLessonsWithPagination;

public record GetLessonsWithPaginationQuery : IRequest<PaginatedList<LessonBriefDto>>, IPaginationQuery
{
    public Guid? CourseId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetLessonsWithPaginationQueryHandler : IRequestHandler<GetLessonsWithPaginationQuery, PaginatedList<LessonBriefDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLessonsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<LessonBriefDto>> Handle(GetLessonsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Lessons.AsNoTracking();
        if (request.CourseId.HasValue)
        {
            query = query.Where(l => l.CourseId == request.CourseId.Value);
        }

        return await query
            .OrderBy(l => l.CourseId)
            .ThenBy(l => l.Order)
            .ThenBy(l => l.Title)
            .ProjectTo<LessonBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}

public class GetLessonsWithPaginationQueryValidator : PaginationQueryValidator<GetLessonsWithPaginationQuery>
{
    public GetLessonsWithPaginationQueryValidator() : base() { }
}
