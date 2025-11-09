using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;

namespace AskAMuslimBackend.Application.Options.Commands.CreateOption;

public record CreateOptionCommand : IRequest<Guid>
{
    public Guid QuestionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}

public class CreateOptionCommandHandler : IRequestHandler<CreateOptionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateOptionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateOptionCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Questions.AnyAsync(q => q.Id == request.QuestionId, cancellationToken);
        if (!exists)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("QuestionId", "Question does not exist.") });
        }

        var entity = new Option
        {
            QuestionId = request.QuestionId,
            Text = request.Text,
            IsCorrect = request.IsCorrect
        };

        _context.Options.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public class CreateOptionCommandValidator : AbstractValidator<CreateOptionCommand>
{
    public CreateOptionCommandValidator()
    {
        RuleFor(x => x.QuestionId).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(500).WithMessage("Text must not exceed 500 characters.");
    }
}
