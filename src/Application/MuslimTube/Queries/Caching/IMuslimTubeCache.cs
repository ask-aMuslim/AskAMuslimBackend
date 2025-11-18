using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.MuslimTube.Queries.Caching;

public interface IMuslimTubeCache
{
    Task<PaginatedList<MuslimTubeChannelBriefDto>?> GetChannelsAsync(string key, CancellationToken ct);
    Task SetChannelsAsync(string key, PaginatedList<MuslimTubeChannelBriefDto> value, CancellationToken ct);

    Task<PaginatedList<MuslimTubeVideoBriefDto>?> GetVideosAsync(string key, CancellationToken ct);
    Task SetVideosAsync(string key, PaginatedList<MuslimTubeVideoBriefDto> value, CancellationToken ct);
    Task InvalidateChannelAsync(Guid channelId, CancellationToken ct);
}
