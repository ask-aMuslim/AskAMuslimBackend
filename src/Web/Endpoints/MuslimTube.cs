using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.MuslimTube.Commands.AddChannel;
using AskAMuslimBackend.Application.MuslimTube.Commands.ResyncChannel;
using AskAMuslimBackend.Application.MuslimTube.Commands.DeleteChannel;
using AskAMuslimBackend.Application.MuslimTube.Queries;
using AskAMuslimBackend.Application.MuslimTube.Queries.GetChannelsWithPagination;
using AskAMuslimBackend.Application.MuslimTube.Queries.GetVideosByChannel;
using AskAMuslimBackend.Application.MuslimTube.Queries.GetHomeVideos;
using AskAMuslimBackend.Application.MuslimTube.Queries.GetVideoById;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AskAMuslimBackend.Web.Endpoints;

public class MuslimTube : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetChannels, "channels").WithName("GetMuslimTubeChannels").WithSummary("List selected YouTube channels");
        groupBuilder.MapGet(GetHomeVideos, "videos").WithName("GetMuslimTubeHomeVideos").WithSummary("List home videos across selected channels with search & filters");
        groupBuilder.MapGet(GetVideoById, "videos/{id:guid}").WithName("GetMuslimTubeVideo").WithSummary("Get a single video by id");
        groupBuilder.MapGet(GetVideosByChannel, "channels/{channelId:guid}/videos").WithName("GetMuslimTubeVideosByChannel").WithSummary("List videos for a selected channel with search & filters");
        groupBuilder.MapPost(AddChannel, "channels").RequireAuthorization().WithName("AddMuslimTubeChannel").WithSummary("Admin: add a YouTube channel by URL or ID");
        groupBuilder.MapPost(ResyncChannel, "channels/{channelId:guid}/resync").RequireAuthorization().WithName("ResyncMuslimTubeChannel").WithSummary("Admin: fetch latest videos for channel");
        groupBuilder.MapDelete(DeleteChannel, "channels/{channelId:guid}").RequireAuthorization().WithName("DeleteMuslimTubeChannel").WithSummary("Admin: soft-delete a channel");
    }

    // GET /MuslimTube/channels
    public async Task<Ok<PaginatedList<MuslimTubeChannelBriefDto>>> GetChannels(ISender sender, [AsParameters] GetMuslimTubeChannelsWithPaginationQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    // GET /MuslimTube/videos (home)
    public async Task<Ok<PaginatedList<MuslimTubeVideoBriefDto>>> GetHomeVideos(ISender sender, [AsParameters] GetMuslimTubeHomeVideosQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    // GET /MuslimTube/videos/{id}
    public async Task<Results<Ok<MuslimTubeVideoBriefDto>, NotFound>> GetVideoById(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetMuslimTubeVideoByIdQuery(id));
        return result != null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    // GET /MuslimTube/channels/{id}/videos
    public async Task<Ok<PaginatedList<MuslimTubeVideoBriefDto>>> GetVideosByChannel(ISender sender, [AsParameters] GetMuslimTubeVideosByChannelQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    // POST /MuslimTube/channels
    public async Task<Created<Guid>> AddChannel(ISender sender, AddMuslimTubeChannelCommand command)
    {
        var id = await sender.Send(command);
        return TypedResults.Created($"/MuslimTube/channels/{id}", id);
    }

    // POST /MuslimTube/channels/{id}/resync
    public async Task<Ok<int>> ResyncChannel(ISender sender, Guid channelId, int maxVideos = 20)
    {
        var added = await sender.Send(new ResyncMuslimTubeChannelCommand(channelId, maxVideos));
        return TypedResults.Ok(added);
    }

    // DELETE /MuslimTube/channels/{id}
    public async Task<Results<Ok, NotFound>> DeleteChannel(ISender sender, Guid channelId)
    {
        var success = await sender.Send(new DeleteMuslimTubeChannelCommand(channelId));
        return success ? TypedResults.Ok() : TypedResults.NotFound();
    }
}
