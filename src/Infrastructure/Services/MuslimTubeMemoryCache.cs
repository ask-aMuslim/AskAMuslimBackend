using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.MuslimTube.Queries;
using AskAMuslimBackend.Application.MuslimTube.Queries.Caching;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent; // Added

namespace AskAMuslimBackend.Infrastructure.Services;

public class MuslimTubeMemoryCache : IMuslimTubeCache
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan ChannelTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan VideoTtl = TimeSpan.FromMinutes(5);

    // Track channel listing keys so we can invalidate them eagerly.
    private static readonly ConcurrentDictionary<string, byte> ChannelKeys = new();

    public MuslimTubeMemoryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<PaginatedList<MuslimTubeChannelBriefDto>?> GetChannelsAsync(string key, CancellationToken ct)
        => Task.FromResult(_cache.TryGetValue(key, out PaginatedList<MuslimTubeChannelBriefDto>? value) ? value : null);

    public Task SetChannelsAsync(string key, PaginatedList<MuslimTubeChannelBriefDto> value, CancellationToken ct)
    {
        _cache.Set(key, value, ChannelTtl);
        ChannelKeys.TryAdd(key, 0);
        return Task.CompletedTask;
    }

    public Task<PaginatedList<MuslimTubeVideoBriefDto>?> GetVideosAsync(string key, CancellationToken ct)
        => Task.FromResult(_cache.TryGetValue(key, out PaginatedList<MuslimTubeVideoBriefDto>? value) ? value : null);

    public Task SetVideosAsync(string key, PaginatedList<MuslimTubeVideoBriefDto> value, CancellationToken ct)
    {
        _cache.Set(key, value, VideoTtl);
        return Task.CompletedTask;
    }

    public Task InvalidateChannelAsync(Guid channelId, CancellationToken ct)
    {
        // Remove all cached channel list pages so subsequent query rebuilds them including the new/updated channel.
        foreach (var key in ChannelKeys.Keys)
        {
            _cache.Remove(key);
            ChannelKeys.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }
}
