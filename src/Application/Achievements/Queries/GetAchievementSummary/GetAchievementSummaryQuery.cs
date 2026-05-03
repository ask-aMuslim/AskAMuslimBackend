using AskAMuslimBackend.Application.Achievements.Queries;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Achievements.Queries.GetAchievementSummary;

public record GetAchievementSummaryQuery : IRequest<Result<AchievementSummaryDto>>;

public class GetAchievementSummaryQueryHandler : IRequestHandler<GetAchievementSummaryQuery, Result<AchievementSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAchievementSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<AchievementSummaryDto>> Handle(GetAchievementSummaryQuery request, CancellationToken cancellationToken)
    {
        var general = await _context.Achievements
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new GeneralAchievementStatsDto
            {
                TotalNewMuslims = g.Sum(x => x.NewMuslims),
                TotalEvents = g.Sum(x => x.EventsHeld),
                TotalDialogues = g.Sum(x => x.Dialogues)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var years = await _context.Achievements
            .AsNoTracking()
            .GroupBy(a => a.HijriYear)
            .Select(g => new AchievementYearStatsDto
            {
                HijriYear = g.Key,
                NewMuslims = g.Sum(x => x.NewMuslims),
                Events = g.Sum(x => x.EventsHeld),
                Dialogues = g.Sum(x => x.Dialogues)
            })
            .OrderByDescending(y => y.HijriYear)
            .ToListAsync(cancellationToken);

        var summary = new AchievementSummaryDto
        {
            General = general ?? new GeneralAchievementStatsDto(),
            Years = years
        };

        return Result<AchievementSummaryDto>.Success(summary);
    }
}
