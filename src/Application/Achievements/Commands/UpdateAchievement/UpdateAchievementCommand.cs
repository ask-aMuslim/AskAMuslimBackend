using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Achievements.Commands.UpdateAchievement;

public record UpdateAchievementCommand : IRequest<Result>
{
    [JsonIgnore]
    public int HijriYear { get; init; }

    [JsonIgnore]
    public int HijriMonth { get; init; }

    public int NewMuslims { get; init; }
    public int Dialogues { get; init; }
    public int CoursesCompleted { get; init; }
    public int ActiveVolunteers { get; init; }
    public int SocialMediaReach { get; init; }
    public int EventsHeld { get; init; }
}

public class UpdateAchievementCommandHandler : IRequestHandler<UpdateAchievementCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateAchievementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateAchievementCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Achievements
            .FirstOrDefaultAsync(a => a.HijriYear == request.HijriYear && a.HijriMonth == request.HijriMonth, cancellationToken);

        if (entity == null)
        {
            return Result.Failure(new[] { "Achievement not found." });
        }

        entity.NewMuslims = request.NewMuslims;
        entity.Dialogues = request.Dialogues;
        entity.CoursesCompleted = request.CoursesCompleted;
        entity.ActiveVolunteers = request.ActiveVolunteers;
        entity.SocialMediaReach = request.SocialMediaReach;
        entity.EventsHeld = request.EventsHeld;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateAchievementCommandValidator : AbstractValidator<UpdateAchievementCommand>
{
    public UpdateAchievementCommandValidator()
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
