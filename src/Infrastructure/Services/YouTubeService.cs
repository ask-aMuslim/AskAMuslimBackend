using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.MuslimTube.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

namespace AskAMuslimBackend.Infrastructure.Services;

internal class YouTubeService : IYouTubeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public YouTubeService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["YouTube:ApiKey"] ?? string.Empty;
    }

    public async Task<TimeSpan?> FetchVideoDurationAsync(string videoId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return null;

        try
        {
            var url = $"https://www.googleapis.com/youtube/v3/videos?part=contentDetails&id={videoId}&key={_apiKey}";
            var doc = await GetJsonAsync(url, cancellationToken);
            if (doc == null) return null;
            if (!doc.RootElement.TryGetProperty("items", out var items)) return null;
            var item = items.EnumerateArray().FirstOrDefault();
            if (item.ValueKind == JsonValueKind.Undefined) return null;
            if (!item.TryGetProperty("contentDetails", out var contentDetails)) return null;
            if (!contentDetails.TryGetProperty("duration", out var durationEl)) return null;
            var durationString = durationEl.GetString();
            if (string.IsNullOrWhiteSpace(durationString)) return null;
            return XmlConvert.ToTimeSpan(durationString);
        }
        catch
        {
            return null;
        }
    }

    public async Task<YouTubeChannelResult?> FetchChannelAsync(string channelInput, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;
        try
        {
            var channelId = ExtractChannelId(channelInput);
            if (!string.IsNullOrEmpty(channelId))
            {
                var url = $"https://www.googleapis.com/youtube/v3/channels?part=snippet&id={channelId}&key={_apiKey}";
                var doc = await GetJsonAsync(url, cancellationToken);
                if (doc == null) return null;
                if (!doc.RootElement.TryGetProperty("items", out var items)) return null;
                var item = items.EnumerateArray().FirstOrDefault();
                if (item.ValueKind == JsonValueKind.Undefined) return null;
                if (!item.TryGetProperty("snippet", out var snippet)) return null;
                return BuildChannelResult(channelId, snippet);
            }
            else
            {
                var query = channelInput.Trim();
                var lastSegment = query.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? query;
                var url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&type=channel&q={Uri.EscapeDataString(lastSegment)}&maxResults=1&key={_apiKey}";
                var doc = await GetJsonAsync(url, cancellationToken);
                if (doc == null) return null;
                if (!doc.RootElement.TryGetProperty("items", out var items)) return null;
                var item = items.EnumerateArray().FirstOrDefault();
                if (item.ValueKind == JsonValueKind.Undefined) return null;
                if (!item.TryGetProperty("snippet", out var snippet)) return null;
                if (!item.TryGetProperty("id", out var idObj) || !idObj.TryGetProperty("channelId", out var channelIdEl)) return null;
                var resolvedChannelId = channelIdEl.GetString() ?? string.Empty;
                if (string.IsNullOrEmpty(resolvedChannelId)) return null;
                return BuildChannelResult(resolvedChannelId, snippet);
            }
        }
        catch
        {
            return null; // swallow and return null to avoid seed crash
        }
    }

    public async Task<IReadOnlyCollection<YouTubeVideoResult>> FetchChannelVideosAsync(string youTubeChannelId, int maxResults, CancellationToken cancellationToken)
    {
        var list = new List<YouTubeVideoResult>();
        if (string.IsNullOrWhiteSpace(_apiKey)) return list;
        try
        {
            var url = $"https://www.googleapis.com/youtube/v3/search?part=snippet&channelId={youTubeChannelId}&maxResults={Math.Min(maxResults, 50)}&order=date&type=video&key={_apiKey}";
            var doc = await GetJsonAsync(url, cancellationToken);
            if (doc == null) return list;
            if (!doc.RootElement.TryGetProperty("items", out var items)) return list;
            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idEl) || !idEl.TryGetProperty("videoId", out var vidEl)) continue;
                var videoId = vidEl.GetString() ?? string.Empty;
                if (!item.TryGetProperty("snippet", out var snippet)) continue;
                DateTimeOffset? publishedAt = null;
                if (snippet.TryGetProperty("publishedAt", out var pubEl) && DateTimeOffset.TryParse(pubEl.GetString(), out var dto))
                {
                    publishedAt = dto;
                }
                list.Add(new YouTubeVideoResult(
                    videoId,
                    snippet.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty,
                    snippet.TryGetProperty("description", out var descEl) ? descEl.GetString() : null,
                    GetThumb(snippet, "default"),
                    GetThumb(snippet, "medium"),
                    GetThumb(snippet, "high"),
                    publishedAt));
            }
        }
        catch
        {
            // ignore errors, return what collected (likely empty)
        }
        return list;
    }

    private static YouTubeChannelResult BuildChannelResult(string channelId, JsonElement snippet) => new(
        channelId,
        snippet.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty,
        snippet.TryGetProperty("description", out var descEl) ? descEl.GetString() : null,
        GetThumb(snippet, "default"),
        GetThumb(snippet, "medium"),
        GetThumb(snippet, "high"));

    private async Task<JsonDocument?> GetJsonAsync(string url, CancellationToken ct)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(url, ct);
            var doc = JsonDocument.Parse(json);
            // if error element exists, treat as null
            if (doc.RootElement.TryGetProperty("error", out _)) return null;
            return doc;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetThumb(JsonElement snippet, string size)
    {
        if (snippet.TryGetProperty("thumbnails", out var thumbs) && thumbs.TryGetProperty(size, out var sz) && sz.TryGetProperty("url", out var url))
        {
            return url.GetString();
        }
        return null;
    }

    private static string? ExtractChannelId(string input)
    {
        var trimmed = input.Trim();
        if (Regex.IsMatch(trimmed, "^UC[0-9A-Za-z_-]{22}$")) return trimmed;
        var match = Regex.Match(trimmed, @"youtube\.com/(channel/)(UC[0-9A-Za-z_-]{22})", RegexOptions.IgnoreCase);
        if (match.Success) return match.Groups[2].Value;
        return null;
    }
}
