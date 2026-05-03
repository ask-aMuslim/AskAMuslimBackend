using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Achievements.Commands.CreateAchievement;

public record CreateAchievementCommand : IRequest<Result<Guid>>
{
    public int HijriYear { get; init; }
    public int HijriMonth { get; init; }
    public int NewMuslims { get; init; }
    public int Dialogues { get; init; }
    public int CoursesCompleted { get; init; }
    public int ActiveVolunteers { get; init; }
    public int SocialMediaReach { get; init; }
    public int EventsHeld { get; init; }
}

public class CreateAchievementCommandHandler : IRequestHandler<CreateAchievementCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateAchievementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAchievementCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Achievements
            .AsNoTracking()
            .AnyAsync(a => a.HijriYear == request.HijriYear && a.HijriMonth == request.HijriMonth, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(new[] { "Achievement already exists for this Hijri month." });
        }

        var entity = new Achievement
        {
            HijriYear = request.HijriYear,
            HijriMonth = request.HijriMonth,
            NewMuslims = request.NewMuslims,
            Dialogues = request.Dialogues,
            CoursesCompleted = request.CoursesCompleted,
            ActiveVolunteers = request.ActiveVolunteers,
            SocialMediaReach = request.SocialMediaReach,
            EventsHeld = request.EventsHeld
        };

        _context.Achievements.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}

public class CreateAchievementCommandValidator : AbstractValidator<CreateAchievementCommand>
{
    public CreateAchievementCommandValidator()
    {
        RuleFor(x => x.HijriYear)
            .GreaterThan(0)
            .WithMessage("Hijri year must be greater than 0.");

        RuleFor(x => x.HijriMonth)
            .InclusiveBetween(1, 12)
            .WithMessage("Hijri month must be between 1 and 12.");

        RuleFor(x => x.NewMuslims).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dialogues).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CoursesCompleted).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ActiveVolunteers).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SocialMediaReach).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EventsHeld).GreaterThanOrEqualTo(0);
    }
}
