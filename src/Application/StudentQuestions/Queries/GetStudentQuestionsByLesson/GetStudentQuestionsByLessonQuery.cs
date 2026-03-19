using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionsByLesson;

public record GetStudentQuestionsByLessonQuery(Guid LessonId) : IRequest<Result<List<StudentQuestionDto>>>;

public class GetStudentQuestionsByLessonQueryHandler : IRequestHandler<GetStudentQuestionsByLessonQuery, Result<List<StudentQuestionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentQuestionsByLessonQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<StudentQuestionDto>>> Handle(GetStudentQuestionsByLessonQuery request, CancellationToken cancellationToken)
    {
        var questions = await _context.StudentQuestions
            .Where(q => q.LessonId == request.LessonId)
            .OrderBy(q => q.Timestamp)
            .Select(s => new StudentQuestionDto {
            Id = s.Id,
            StudentId = s.StudentId,
            LessonId = s.LessonId,
            QuestionText = s.QuestionText,
            Timestamp = s.Timestamp,
            Answered = s.Answered,
            AnswerText = s.AnswerText
    })
            .ToListAsync(cancellationToken);
        
        return Result<List<StudentQuestionDto>>.Success(questions);
    }
}
