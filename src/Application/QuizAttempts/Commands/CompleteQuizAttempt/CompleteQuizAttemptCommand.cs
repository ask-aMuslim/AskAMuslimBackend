using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QuizAttempts.Commands.CompleteQuizAttempt;

public record CompleteQuizAttemptCommand : IRequest
{
    public Guid AttemptId { get; init; }
    public decimal Score { get; init; }
    public bool IsPassed { get; init; }
}

public class CompleteQuizAttemptCommandHandler : IRequestHandler<CompleteQuizAttemptCommand>
{
    private readonly IApplicationDbContext _context;

    public CompleteQuizAttemptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(CompleteQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        var attempt = await _context.QuizAttempts.FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken);
        Guard.Against.NotFound(request.AttemptId, attempt);

        attempt.Score = request.Score;
        attempt.IsPassed = request.IsPassed;
        attempt.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class CompleteQuizAttemptCommandValidator : AbstractValidator<CompleteQuizAttemptCommand>
{
    public CompleteQuizAttemptCommandValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.Score).GreaterThanOrEqualTo(0);
    }
}
