using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;

namespace AskAMuslimBackend.Application.Options.Commands.CreateOption;

public record CreateOptionCommand : IRequest<Result<Guid>>
{
    public Guid QuestionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
    public string? Explanation { get; init; }
}

public class CreateOptionCommandHandler : IRequestHandler<CreateOptionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public CreateOptionCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(CreateOptionCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Questions.AnyAsync(q => q.Id == request.QuestionId, cancellationToken);
        if (!exists)
        {
            return Result<Guid>.Failure(new[] { _localizer["Questions.DoesNotExist"] });
        }

        var entity = new Option
        {
            QuestionId = request.QuestionId,
            Text = request.Text,
            IsCorrect = request.IsCorrect,
            Explanation = request.Explanation ?? string.Empty
        };

        _context.Options.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
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
        RuleFor(x => x.Explanation)
            .MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Explanation))
            .WithMessage("Explanation must not exceed 1000 characters.");
    }
}
