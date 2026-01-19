using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Statistics.GetStatistics;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Statistics : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetStatistics)
            .RequireAuthorization()
            .WithName("GetStatistics")
            .WithSummary("Get platform statistics including student, course, and enrollment counts");
    }

    public async Task<IResult> GetStatistics(ISender sender)
    {
        var result = await sender.Send(new GetStatisticsQuery());
        return result.ToHttpResult();
    }
}
