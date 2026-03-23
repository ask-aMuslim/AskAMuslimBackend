using AskAMuslimBackend.Application.Languages.Queries.GetLanguages;
using AskAMuslimBackend.Web.Infrastructure;

namespace AskAMuslimBackend.Web.Endpoints;

public class Languages : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetLanguages)
            .AllowAnonymous()
            .WithName("GetLanguages")
            .WithSummary("Get all languages and their enum values");
    }

    public async Task<IResult> GetLanguages(ISender sender)
    {
        var result = await sender.Send(new GetLanguagesQuery());
        return result.ToHttpResult();
    }
}
