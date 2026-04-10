using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Commands.CreateQuizAttempt;

public record CreateQuizAttemptCommand : IRequest<Result<Guid>>
{
    public Guid QuizId { get; init; }
    public Guid StudentId { get; init; }
}

public class CreateQuizAttemptCommandHandler : IRequestHandler<CreateQuizAttemptCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateQuizAttemptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Lesson)
            .FirstOrDefaultAsync(q => q.Id == request.QuizId, cancellationToken);
            
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);

        if (quiz == null || !studentExists)
        {
            var errors = new List<string>();
            if (quiz == null) errors.Add("Quiz does not exist.");
            if (!studentExists) errors.Add("Student does not exist.");
            return Result<Guid>.Failure(errors.ToArray());
        }
        
        // Progression check for Lesson Quiz: Must pass the previous lesson's quiz first
        if (quiz.TargetType == Domain.Enums.QuizTargetType.Lesson && quiz.Lesson != null)
        {
            var previousLesson = await _context.Lessons
                .Where(l => l.CourseId == quiz.Lesson.CourseId && l.Order < quiz.Lesson.Order)
                .OrderByDescending(l => l.Order)
                .FirstOrDefaultAsync(cancellationToken);

            if (previousLesson != null && previousLesson.HasQuiz)
            {
                bool passedPreviousQuiz = await _context.QuizAttempts
                    .AnyAsync(a => a.StudentId == request.StudentId && 
                                   a.Quiz != null && 
                                   a.Quiz.LessonId == previousLesson.Id && 
                                   a.Quiz.TargetType == Domain.Enums.QuizTargetType.Lesson && 
                                   a.IsPassed, cancellationToken);

                if (!passedPreviousQuiz)
                {
                    return Result<Guid>.Failure(new[] { $"You must pass the quiz for the previous lesson '{previousLesson.Title}' before attempting this one." });
                }
            }
        }

        // Determine next attempt number for this student/quiz (robust across providers)
        var lastAttemptNumber = await _context.QuizAttempts
            .Where(a => a.QuizId == request.QuizId && a.StudentId == request.StudentId)
            .OrderByDescending(a => a.AttemptNumber)
            .Select(a => (int?)a.AttemptNumber)
            .FirstOrDefaultAsync(cancellationToken) ?? 0;

        var entity = new QuizAttempt
        {
            QuizId = request.QuizId,
            StudentId = request.StudentId,
            AttemptNumber = lastAttemptNumber + 1,
            StartedAt = DateTime.UtcNow,
            IsPassed = false,
            Score = 0m
        };

        _context.QuizAttempts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}

public class CreateQuizAttemptCommandValidator : AbstractValidator<CreateQuizAttemptCommand>
{
    public CreateQuizAttemptCommandValidator()
    {
        RuleFor(x => x.QuizId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
    }
}
