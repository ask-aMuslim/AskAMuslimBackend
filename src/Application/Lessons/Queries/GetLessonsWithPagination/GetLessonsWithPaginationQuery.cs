using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;
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
    public Language? Language { get; init; }
    
}

public class GetLessonsWithPaginationQueryHandler : IRequestHandler<GetLessonsWithPaginationQuery, Result<PaginatedList<LessonBriefDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLessonsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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

        if (request.Language.HasValue)
        {
            query = query.Where(l => l.Language == request.Language.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(l => 
                l.Title.ToLower().Contains(searchLower) ||
                (l.Description != null && l.Description.ToLower().Contains(searchLower)));
        }

        var paginatedList = await query
            .Include(l => l.Course)
                .ThenInclude(c => c!.Level)
            .OrderBy(l => l.CourseId)
            .ThenBy(l => l.Order)
            .ThenBy(l => l.Title)
            .Select(s => new LessonBriefDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                Title = s.Title,
                Description = s.Description,
                ContentJson = s.ContentJson,
                Type = s.Type,
                Order = s.Order,
                ThumbnailUrl = s.ThumbnailUrl,
                ContentUrl = s.ContentUrl,
                Transcript = s.Transcript,
                IsPublished = s.IsPublished,
                HasQuiz = s.HasQuiz,
                Duration = s.Duration,
                LevelName = s.Course!.Level != null ? s.Course.Level.Title : null,
                Language = s.Language
            })
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
