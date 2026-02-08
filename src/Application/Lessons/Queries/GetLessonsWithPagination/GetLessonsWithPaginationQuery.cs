using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Lessons.Queries.GetLessonsWithPagination;

public record GetLessonsWithPaginationQuery : IRequest<Result<PaginatedList<LessonBriefDto>>>, IPaginationQuery
{
    public Guid? CourseId { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsPublished { get; init; }
    public LessonType? Type { get; init; }
}

public class GetLessonsWithPaginationQueryHandler : IRequestHandler<GetLessonsWithPaginationQuery, Result<PaginatedList<LessonBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetLessonsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<LessonBriefDto>>> Handle(GetLessonsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Lessons.AsNoTracking();
        
        if (request.CourseId.HasValue)
        {
            query = query.Where(l => l.CourseId == request.CourseId.Value);
        }

        if (request.IsPublished.HasValue)
        {
            query = query.Where(l => l.IsPublished == request.IsPublished.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(l => l.Type == request.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(l => 
                l.Title.Contains(request.SearchTerm) ||
                (l.Description != null && l.Description.Contains(request.SearchTerm)));
        }

        var paginatedList = await query
            .OrderBy(l => l.CourseId)
            .ThenBy(l => l.Order)
            .ThenBy(l => l.Title)
            .ProjectTo<LessonBriefDto>(_mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
        
        return Result<PaginatedList<LessonBriefDto>>.Success(paginatedList);
    }
}

public class GetLessonsWithPaginationQueryValidator : PaginationQueryValidator<GetLessonsWithPaginationQuery>
{
    public GetLessonsWithPaginationQueryValidator() : base()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue);
    }
}
