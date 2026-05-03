using AskAMuslimBackend.Application.Achievements.Queries;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Achievements.Queries.GetAchievementYear;

public record GetAchievementYearQuery(int HijriYear) : IRequest<Result<AchievementYearDto>>;

public class GetAchievementYearQueryHandler : IRequestHandler<GetAchievementYearQuery, Result<AchievementYearDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAchievementYearQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AchievementYearDto>> Handle(GetAchievementYearQuery request, CancellationToken cancellationToken)
    {
        var months = await _context.Achievements
            .AsNoTracking()
            .Where(a => a.HijriYear == request.HijriYear)
            .OrderBy(a => a.HijriMonth)
            .Select(a => new AchievementMonthDto
            {
                HijriMonth = a.HijriMonth,
                NewMuslims = a.NewMuslims,
                Dialogues = a.Dialogues,
                CoursesCompleted = a.CoursesCompleted,
                ActiveVolunteers = a.ActiveVolunteers,
                SocialMediaReach = a.SocialMediaReach,
                EventsHeld = a.EventsHeld
            })
            .ToListAsync(cancellationToken);

        var totals = new AchievementYearStatsDto
        {
            HijriYear = request.HijriYear,
            NewMuslims = months.Sum(m => m.NewMuslims),
            Events = months.Sum(m => m.EventsHeld),
            Dialogues = months.Sum(m => m.Dialogues)
        };

        var result = new AchievementYearDto
        {
            HijriYear = request.HijriYear,
            Totals = totals,
            Months = months
        };

        return Result<AchievementYearDto>.Success(result);
    }
}

public class GetAchievementYearQueryValidator : AbstractValidator<GetAchievementYearQuery>
{
    public GetAchievementYearQueryValidator()
    {
        RuleFor(x => x.HijriYear)
            .GreaterThan(0)
            .WithMessage("Hijri year must be greater than 0.");
    }
}
