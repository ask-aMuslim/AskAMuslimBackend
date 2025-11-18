using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestions;

public record GetStudentQuestionsQuery : IRequest<List<StudentQuestionDto>>
{
    public Guid StudentId { get; init; }
}

public class GetStudentQuestionsQueryHandler : IRequestHandler<GetStudentQuestionsQuery, List<StudentQuestionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStudentQuestionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<StudentQuestionDto>> Handle(GetStudentQuestionsQuery request, CancellationToken cancellationToken)
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

        return questions;
    }
}
