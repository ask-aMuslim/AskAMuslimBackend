using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Levels.Commands.CreateLevel;

public record CreateLevelCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Order { get; init; }
    public LevelDifficulty Difficulty { get; init; }    public bool IsPublished { get; init; } = false;}

public class CreateLevelCommandHandler : IRequestHandler<CreateLevelCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateLevelCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateLevelCommand request, CancellationToken cancellationToken)
    {
        var level = new Level
        {
            Title = request.Title,
            Description = request.Description,
            Order = request.Order,
            Difficulty = request.Difficulty,
            IsPublished = request.IsPublished
        };

        _context.Levels.Add(level);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(level.Id);
    }
}

public class CreateLevelCommandValidator : AbstractValidator<CreateLevelCommand>
{
    public CreateLevelCommandValidator()
    {
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
