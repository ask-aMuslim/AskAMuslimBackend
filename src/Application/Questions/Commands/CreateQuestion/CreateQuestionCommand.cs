using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;

namespace AskAMuslimBackend.Application.Questions.Commands.CreateQuestion;

public record CreateQuestionCommand : IRequest<Result<Guid>>
{
    public Guid QuizId { get; init; }
    public string Text { get; init; } = string.Empty;
    public int? Order { get; init; }
}

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Quizzes.AnyAsync(q => q.Id == request.QuizId, cancellationToken);
        if (!exists)
        {
            return Result<Guid>.Failure(new[] { "Quiz does not exist." });
        }

        // Auto-assign Order if omitted
        var order = request.Order ?? 0;
        if (!request.Order.HasValue)
        {
            var maxOrder = await _context.Questions
                .Where(q => q.QuizId == request.QuizId)
                .Select(q => (int?)q.Order)
                .OrderByDescending(o => o)
                .FirstOrDefaultAsync(cancellationToken);
            order = (maxOrder ?? 0) + 1;
        }

        var entity = new Question
        {
            QuizId = request.QuizId,
            Text = request.Text,
            Order = order
        };

        _context.Questions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}

public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
    {
        RuleFor(x => x.QuizId).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(1000).WithMessage("Text must not exceed 1000 characters.");
    }
}
