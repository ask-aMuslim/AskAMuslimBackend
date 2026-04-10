using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Levels.Queries.GetLevelById;

public record GetLevelByIdQuery(Guid Id) : IRequest<Result<LevelBriefDto?>>;

public class GetLevelByIdQueryHandler : IRequestHandler<GetLevelByIdQuery, Result<LevelBriefDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetLevelByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<LevelBriefDto?>> Handle(GetLevelByIdQuery request, CancellationToken cancellationToken)
    {
        var level = await _context.Levels
            .Where(l => l.Id == request.Id)
            .Select(s => new LevelBriefDto {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Order = s.Order,
            Difficulty = s.Difficulty,
            IsPublished = s.IsPublished,
            Language = s.Language,
            TotalCourses = s.Courses.Count,
            HasQuiz = s.HasQuiz,
            CanAccessLevel = true
    })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (level != null && !string.IsNullOrWhiteSpace(_user.Id) && Guid.TryParse(_user.Id, out var studentId))
        {
            var previousLevelWithQuiz = await _context.Levels
                .Where(l => l.Language == level.Language && l.Order < level.Order && l.HasQuiz)
                .OrderByDescending(l => l.Order)
                .Select(l => new { l.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (previousLevelWithQuiz != null)
            {
                level.CanAccessLevel = await _context.QuizAttempts
                    .AnyAsync(a => a.StudentId == studentId && 
                                   a.Quiz != null && 
                                   a.Quiz.TargetType == Domain.Enums.QuizTargetType.Level && 
                                   a.Quiz.LevelId == previousLevelWithQuiz.Id && 
                                   a.IsPassed, cancellationToken);
            }
        }
        
        return Result<LevelBriefDto?>.Success(level);
    }
}
