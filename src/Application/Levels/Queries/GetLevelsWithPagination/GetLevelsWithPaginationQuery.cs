using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelsWithPagination;

public record GetLevelsWithPaginationQuery : IRequest<Result<PaginatedList<LevelBriefDto>>>, IPaginationQuery
{
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public bool? IsPublished { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Language? Language { get; init; }
}

public class GetLevelsWithPaginationQueryHandler : IRequestHandler<GetLevelsWithPaginationQuery, Result<PaginatedList<LevelBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetLevelsWithPaginationQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<PaginatedList<LevelBriefDto>>> Handle(GetLevelsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Levels.AsQueryable();

        if (request.Language.HasValue)
        {
            query = query.Where(l => l.Language == request.Language.Value);
        }

        if (request.IsPublished.HasValue)
        {
            query = query.Where(l => l.IsPublished == request.IsPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(l => 
                l.Title.Contains(request.SearchTerm) ||
                (l.Description != null && l.Description.Contains(request.SearchTerm)));
        }

        var paginatedList = await query
            .OrderBy(l => l.Order)
            .ThenBy(l => l.Title)
            .Select(s => new LevelBriefDto {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Order = s.Order,
            Difficulty = s.Difficulty,
            IsPublished = s.IsPublished,
            Language = s.Language,
            HasQuiz = s.HasQuiz,
            CanAccessLevel = true,
            TotalCourses = s.Courses.Count
    })
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);
            
        if (!string.IsNullOrWhiteSpace(_user.Id) && Guid.TryParse(_user.Id, out var studentId))
        {
            var orderedLevelsInfo = await _context.Levels
                .Where(l => !request.Language.HasValue || l.Language == request.Language.Value)
                .Select(l => new { l.Id, l.Order, l.HasQuiz })
                .OrderBy(l => l.Order)
                .ToListAsync(cancellationToken);

            var passedQuizLevelIds = await _context.QuizAttempts
                .Where(a => a.StudentId == studentId && 
                            a.Quiz != null && 
                            a.Quiz.TargetType == Domain.Enums.QuizTargetType.Level && 
                            a.IsPassed)
                .Select(a => a.Quiz!.LevelId)
                .ToListAsync(cancellationToken);

            foreach (var item in paginatedList.Items)
            {
                var previousLevelWithQuiz = orderedLevelsInfo
                    .Where(l => l.Order < item.Order && l.HasQuiz)
                    .OrderByDescending(l => l.Order)
                    .FirstOrDefault();

                if (previousLevelWithQuiz != null)
                {
                    item.CanAccessLevel = passedQuizLevelIds.Contains(previousLevelWithQuiz.Id);
                }
            }
        }
            
        return Result<PaginatedList<LevelBriefDto>>.Success(paginatedList);
    }
}
