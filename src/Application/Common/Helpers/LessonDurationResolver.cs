using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using System.Text.RegularExpressions;

namespace AskAMuslimBackend.Application.Common.Helpers;

public static class LessonDurationResolver
{
    private static readonly TimeSpan DefaultContentDuration = new(0, 5, 0); // 5 minutes

    public static async Task<TimeSpan?> ResolveAsync(
        LessonType type,
        string? contentUrl,
        TimeSpan? explicitDuration,
        IYouTubeService youTubeService,
        CancellationToken cancellationToken)
    {
        // If admin provided an explicit duration, respect it
        if (explicitDuration.HasValue)
            return explicitDuration.Value;

        // Auto-resolve based on type
        switch (type)
        {
            case LessonType.Video:
                if (!string.IsNullOrWhiteSpace(contentUrl))
                {
                    var videoId = ExtractYouTubeVideoId(contentUrl);
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        return await youTubeService.FetchVideoDurationAsync(videoId, cancellationToken);
                    }
                }
                return null;

            case LessonType.Audio:
                if (!string.IsNullOrWhiteSpace(contentUrl))
                {
                    var audioDuration = await youTubeService.FetchAudioDurationAsync(contentUrl, cancellationToken);
                    if (audioDuration.HasValue) return audioDuration.Value;
                }
                return DefaultContentDuration;

            case LessonType.Article:
            case LessonType.Document:
                return DefaultContentDuration;

            default:
                return null;
        }
    }

    private static string? ExtractYouTubeVideoId(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var shortMatch = Regex.Match(url, @"youtu\.be/([a-zA-Z0-9_-]{11})");
        if (shortMatch.Success) return shortMatch.Groups[1].Value;
        var normalMatch = Regex.Match(url, @"[?&]v=([a-zA-Z0-9_-]{11})");
        if (normalMatch.Success) return normalMatch.Groups[1].Value;
        var embedMatch = Regex.Match(url, @"youtube\.com/embed/([a-zA-Z0-9_-]{11})");
        if (embedMatch.Success) return embedMatch.Groups[1].Value;
        return null;
    }
}
