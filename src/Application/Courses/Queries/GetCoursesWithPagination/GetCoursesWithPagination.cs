using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCoursesWithPagination;

public record GetCoursesWithPaginationQuery : IRequest<Result<PaginatedList<CourseBriefDto>>>, IPaginationQuery
{
    public Guid? LevelId { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsPublished { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Language? Language { get; init; }
}

public class GetCoursesWithPaginationQueryHandler : IRequestHandler<GetCoursesWithPaginationQuery, Result<PaginatedList<CourseBriefDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCoursesWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<CourseBriefDto>>> Handle(GetCoursesWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Courses
            .Include(c => c.CourseTags)
                .ThenInclude(ct => ct.Tag)
            .AsNoTracking();
        
        if (request.LevelId.HasValue)
        {
            query = query.Where(c => c.LevelId == request.LevelId.Value);
        }

        if (request.Language.HasValue)
        {
            query = query.Where(c => c.Language == request.Language.Value);
        }

        if (request.IsPublished.HasValue)
        {
            query = query.Where(c => c.IsPublished == request.IsPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(c => 
                c.Title.Contains(request.SearchTerm) ||
                (c.Description != null && c.Description.Contains(request.SearchTerm)));
        }

        var paginatedList = await query
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Title)
            .Select(s => new CourseBriefDto {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            ThumbnailUrl = s.ThumbnailUrl,
            IsPublished = s.IsPublished,
            Rating = s.Rating,
            Order = s.Order,
            Language = s.Language,
            Tags = s.CourseTags.Select(ct => new AskAMuslimBackend.Application.Tags.Queries.TagBriefDto { Id = ct.Tag!.Id, Title = ct.Tag.Title, Description = ct.Tag.Description, TotalCourses = ct.Tag.CourseTags.Count, TotalQAs = ct.Tag.QATags.Count }).ToList()
    })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
            
        return Result<PaginatedList<CourseBriefDto>>.Success(paginatedList);
    }
}
