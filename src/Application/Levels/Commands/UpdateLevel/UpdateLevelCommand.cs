using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Levels.Commands.UpdateLevel;

public record UpdateLevelCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Order { get; init; }
    public LevelDifficulty Difficulty { get; init; }    public bool IsPublished { get; init; }
    public Language Language { get; init; } = Language.English;
}

public class UpdateLevelCommandHandler : IRequestHandler<UpdateLevelCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public UpdateLevelCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Levels.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
        {
            return Result.Failure(new[] { _localizer["Levels.NotFound"] });
        }

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Order = request.Order;
        entity.Difficulty = request.Difficulty;
        entity.IsPublished = request.IsPublished;
        entity.Language = request.Language;

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}

public class UpdateLevelCommandValidator : AbstractValidator<UpdateLevelCommand>
{
    public UpdateLevelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Level Id is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be 0 or greater.");

        RuleFor(x => x.Difficulty)
            .IsInEnum().WithMessage("Invalid difficulty value.");

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Invalid language value.");
    }
}
