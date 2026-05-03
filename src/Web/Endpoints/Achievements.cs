using AskAMuslimBackend.Application.Achievements.Commands.CreateAchievement;
using AskAMuslimBackend.Application.Achievements.Commands.UpdateAchievement;
using AskAMuslimBackend.Application.Achievements.Queries.GetAchievementSummary;
using AskAMuslimBackend.Application.Achievements.Queries.GetAchievementYear;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Achievements : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAchievementSummary)
            .WithName("GetAchievementSummary")
            .WithSummary("Get general achievements totals and yearly stats");

        groupBuilder.MapGet(GetAchievementYear, "{hijriYear:int}")
            .WithName("GetAchievementYear")
            .WithSummary("Get achievement stats for a Hijri year, including monthly breakdown");

        groupBuilder.MapPost(CreateAchievement)
            .WithName("CreateAchievement")
            .WithSummary("Create achievement stats for a Hijri month");

        groupBuilder.MapPut(UpdateAchievement, "{hijriYear:int}/{hijriMonth:int}")
            .WithName("UpdateAchievement")
            .WithSummary("Update achievement stats for a Hijri month");
    }

    public async Task<IResult> GetAchievementSummary(ISender sender)
    {
        var result = await sender.Send(new GetAchievementSummaryQuery());
        return result.ToHttpResult();
    }

    public async Task<IResult> GetAchievementYear(ISender sender, int hijriYear)
    {
        var result = await sender.Send(new GetAchievementYearQuery(hijriYear));
        return result.ToHttpResult();
    }

    public async Task<IResult> CreateAchievement(ISender sender, CreateAchievementCommand command)
    {
        var result = await sender.Send(command);
        return result.ToCreatedResult($"/{nameof(Achievements)}/{result.Data}");
    }

    public async Task<IResult> UpdateAchievement(ISender sender, int hijriYear, int hijriMonth, UpdateAchievementCommand command)
    {
        var result = await sender.Send(command with { HijriYear = hijriYear, HijriMonth = hijriMonth });
        return result.ToHttpResult();
    }
}
