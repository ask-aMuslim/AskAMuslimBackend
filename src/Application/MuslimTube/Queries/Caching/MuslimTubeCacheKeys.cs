namespace AskAMuslimBackend.Application.MuslimTube.Queries.Caching;

public static class MuslimTubeCacheKeys
{
    public static string Channels(int page, int size) => $"mt:channels:{page}:{size}";
    public static string Videos(Guid channelId, int page, int size, string? search, DateTimeOffset? after, DateTimeOffset? before)
        => $"mt:videos:{channelId}:{page}:{size}:{search}:{after?.ToUnixTimeSeconds()}:{before?.ToUnixTimeSeconds()}";
    public static string HomeVideos(int page, int size, string? search, bool randomize, DateTimeOffset? after, DateTimeOffset? before)
        => $"mt:home:{page}:{size}:{search}:{randomize}:{after?.ToUnixTimeSeconds()}:{before?.ToUnixTimeSeconds()}";
}
