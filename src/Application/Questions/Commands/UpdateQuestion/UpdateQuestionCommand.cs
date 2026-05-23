using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Questions.Commands.UpdateQuestion;

public record UpdateQuestionCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int? Order { get; init; }
}

public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public UpdateQuestionCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Questions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) return Result.Failure(new[] { _localizer["Questions.NotFound"] });

        entity.Text = request.Text;
        if (request.Order.HasValue)
        {
            entity.Order = request.Order.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateQuestionCommandValidator : AbstractValidator<UpdateQuestionCommand>
{
    public UpdateQuestionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(1000).WithMessage("Text must not exceed 1000 characters.");
    }
}
