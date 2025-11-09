using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;

namespace AskAMuslimBackend.Application.Questions.Commands.CreateQuestion;

public record CreateQuestionCommand : IRequest<Guid>
{
    public Guid QuizId { get; init; }
    public string Text { get; init; } = string.Empty;
}

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Quizzes.AnyAsync(q => q.Id == request.QuizId, cancellationToken);
        if (!exists)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("QuizId", "Quiz does not exist.") });
        }

        var entity = new Question
        {
            QuizId = request.QuizId,
            Text = request.Text
        };

        _context.Questions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
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
