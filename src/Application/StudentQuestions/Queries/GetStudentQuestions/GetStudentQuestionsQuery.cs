using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestions;

public record GetStudentQuestionsQuery : IRequest<Result<List<StudentQuestionDto>>>
{
    public Guid StudentId { get; init; }
}

public class GetStudentQuestionsQueryHandler : IRequestHandler<GetStudentQuestionsQuery, Result<List<StudentQuestionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentQuestionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<StudentQuestionDto>>> Handle(GetStudentQuestionsQuery request, CancellationToken cancellationToken)
    {
        var questions = await _context.StudentQuestions
            .Where(q => q.StudentId == request.StudentId)
            .OrderByDescending(q => q.Created)
            .Select(q => new StudentQuestionDto
            {
                Id = q.Id,
                StudentId = q.StudentId,
                QuestionText = q.QuestionText,
                AnswerText = q.AnswerText,
                IsAnswered = q.Answered,
                AnsweredAt = q.AnsweredAt,
                CreatedAt = q.Created
            })
            .ToListAsync(cancellationToken);

        return Result<List<StudentQuestionDto>>.Success(questions);
    }
}
