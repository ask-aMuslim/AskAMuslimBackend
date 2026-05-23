using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptById;

public record GetQuizAttemptByIdQuery(Guid Id) : IRequest<Result<QuizAttemptDetailDto?>>;

public class GetQuizAttemptByIdQueryHandler : IRequestHandler<GetQuizAttemptByIdQuery, Result<QuizAttemptDetailDto?>>
{
    private readonly IApplicationDbContext _context;

    public GetQuizAttemptByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<QuizAttemptDetailDto?>> Handle(GetQuizAttemptByIdQuery request, CancellationToken cancellationToken)
    {
        // Load the attempt with quiz, student, and answers
        var attempt = await _context.QuizAttempts
            .AsNoTracking()
            .Include(a => a.Quiz)
                .ThenInclude(q => q!.Questions)
                    .ThenInclude(q => q.Options)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (attempt == null)
        {
            return Result<QuizAttemptDetailDto?>.Failure(new[] { "Quiz attempt not found." });
        }

        // Resolve student name via LINQ join
        var studentName = string.Empty;
        var studentUser = await (from s in _context.Students
                                 join u in _context.Users on s.UserId equals u.Id
                                 where s.Id == attempt.StudentId
                                 select u)
                                .FirstOrDefaultAsync(cancellationToken);
        if (studentUser != null)
        {
            studentName = $"{studentUser.FirstName} {studentUser.LastName}".Trim();
        }

        // Map questions with options and student answers
        var questionDtos = new List<QuizAttemptQuestionDto>();
        if (attempt.Quiz?.Questions != null)
        {
            var answersByQuestion = attempt.Answers.ToDictionary(a => a.QuestionId);

            foreach (var question in attempt.Quiz.Questions.OrderBy(q => q.Order).ThenBy(q => q.Created))
            {
                answersByQuestion.TryGetValue(question.Id, out var studentAnswer);

                var options = question.Options
                    .OrderBy(o => o.Created)
                    .Select(o => new QuizAttemptOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        Explanation = string.IsNullOrWhiteSpace(o.Explanation) ? null : o.Explanation
                    })
                    .ToList();

                questionDtos.Add(new QuizAttemptQuestionDto
                {
                    QuestionId = question.Id,
                    Text = question.Text,
                    Order = question.Order,
                    Options = options,
                    SelectedOptionId = studentAnswer?.SelectedOptionId,
                    IsCorrect = studentAnswer?.IsCorrect ?? false
                });
            }
        }

        var totalQuestions = questionDtos.Count;
        var correctAnswers = questionDtos.Count(q => q.IsCorrect);

        var dto = new QuizAttemptDetailDto
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz?.Title ?? string.Empty,
            StudentId = attempt.StudentId,
            StudentName = studentName,
            Score = attempt.Score,
            IsPassed = attempt.IsPassed,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            AttemptNumber = attempt.AttemptNumber,
            TotalQuestions = totalQuestions,
            CorrectAnswersCount = correctAnswers,
            Questions = questionDtos
        };

        return Result<QuizAttemptDetailDto?>.Success(dto);
    }
}
