using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestionById;

public record GetStudentQuestionByIdQuery(Guid Id) : IRequest<Result<StudentQuestionDto?>>;

public class GetStudentQuestionByIdQueryHandler : IRequestHandler<GetStudentQuestionByIdQuery, Result<StudentQuestionDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentQuestionByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<StudentQuestionDto?>> Handle(GetStudentQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var question = await _context.StudentQuestions
            .Where(q => q.Id == request.Id)
            .Select(s => new StudentQuestionDto {
            Id = s.Id,
            StudentId = s.StudentId,
            LessonId = s.LessonId,
            QuestionText = s.QuestionText,
            Timestamp = s.Timestamp,
            Answered = s.Answered,
            AnswerText = s.AnswerText
    })
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<StudentQuestionDto?>.Success(question);
    }
}
