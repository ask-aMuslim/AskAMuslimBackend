using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Levels.Commands.UpdateLevel;

public record UpdateLevelCommand : IRequest
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Order { get; init; }
    public LevelDifficulty Difficulty { get; init; }
}

public class UpdateLevelCommandHandler : IRequestHandler<UpdateLevelCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateLevelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Levels.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.Order = request.Order;
        entity.Difficulty = request.Difficulty;

        await _context.SaveChangesAsync(cancellationToken);
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
    }
}
