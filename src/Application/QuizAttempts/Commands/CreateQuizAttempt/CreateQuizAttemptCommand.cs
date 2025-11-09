using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Commands.CreateQuizAttempt;

public record CreateQuizAttemptCommand : IRequest<Guid>
{
    public Guid QuizId { get; init; }
    public Guid StudentId { get; init; }
}

public class CreateQuizAttemptCommandHandler : IRequestHandler<CreateQuizAttemptCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateQuizAttemptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        var quizExists = await _context.Quizzes.AnyAsync(q => q.Id == request.QuizId, cancellationToken);
        var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId, cancellationToken);

        if (!quizExists || !studentExists)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            if (!quizExists) failures.Add(new("QuizId", "Quiz does not exist."));
            if (!studentExists) failures.Add(new("StudentId", "Student does not exist."));
            throw new ValidationException(failures);
        }

        // Determine next attempt number for this student/quiz
        var attemptNumber = await _context.QuizAttempts
            .Where(a => a.QuizId == request.QuizId && a.StudentId == request.StudentId)
            .Select(a => a.AttemptNumber)
            .DefaultIfEmpty(0)
            .MaxAsync(cancellationToken) + 1;

        var entity = new QuizAttempt
        {
            QuizId = request.QuizId,
            StudentId = request.StudentId,
            AttemptNumber = attemptNumber,
            StartedAt = DateTime.UtcNow,
            IsPassed = false,
            Score = 0m
        };

        _context.QuizAttempts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
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
