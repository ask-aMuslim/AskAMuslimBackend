using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Quizzes.Queries.GetQuizzesWithPagination;

public record GetQuizzesWithPaginationQuery : IRequest<Result<PaginatedList<QuizBriefDto>>>, IPaginationQuery
{
    public Guid? LevelId { get; init; }
    public Guid? CourseId { get; init; }
    public Guid? LessonId { get; init; }
    public QuizTargetType? TargetType { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}

public class GetQuizzesWithPaginationQueryHandler : IRequestHandler<GetQuizzesWithPaginationQuery, Result<PaginatedList<QuizBriefDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetQuizzesWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<QuizBriefDto>>> Handle(GetQuizzesWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Quizzes.AsQueryable();

        if (request.LevelId.HasValue)
        {
            query = query.Where(q => q.LevelId == request.LevelId.Value);
        }
        if (request.CourseId.HasValue)
        {
            query = query.Where(q => q.CourseId == request.CourseId.Value);
        }
        if (request.LessonId.HasValue)
        {
            query = query.Where(q => q.LessonId == request.LessonId.Value);
        }
        if (request.TargetType.HasValue)
        {
            query = query.Where(q => q.TargetType == request.TargetType.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(q => q.Title.Contains(request.SearchTerm));
        }

        var result = await query
            .OrderBy(q => q.Title)
            .Select(s => new QuizBriefDto {
            Id = s.Id,
            LevelId = s.LevelId,
            CourseId = s.CourseId,
            LessonId = s.LessonId,
            TargetType = s.TargetType,
            Title = s.Title,
            TotalMarks = s.TotalMarks,
            QuestionsCount = s.Questions.Count
    })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
        return Result<PaginatedList<QuizBriefDto>>.Success(result);
    }
}
