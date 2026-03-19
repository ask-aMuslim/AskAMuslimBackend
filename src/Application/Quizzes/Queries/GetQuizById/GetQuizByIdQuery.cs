using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Quizzes.Queries.GetQuizById;

public record GetQuizByIdQuery(Guid Id) : IRequest<Result<QuizBriefDto?>>;

public class GetQuizByIdQueryHandler : IRequestHandler<GetQuizByIdQuery, Result<QuizBriefDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetQuizByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<QuizBriefDto?>> Handle(GetQuizByIdQuery request, CancellationToken cancellationToken)
    {
        var quiz = await _context.Quizzes
            .Where(q => q.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);
        return Result<QuizBriefDto?>.Success(quiz);
    }
}
