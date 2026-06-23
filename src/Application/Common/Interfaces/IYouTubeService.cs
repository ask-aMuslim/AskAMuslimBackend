using AskAMuslimBackend.Application.MuslimTube.Models;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IYouTubeService
{
    Task<YouTubeChannelResult?> FetchChannelAsync(string channelInput, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<YouTubeVideoResult>> FetchChannelVideosAsync(string youTubeChannelId, int maxResults, CancellationToken cancellationToken);
    Task<TimeSpan?> FetchVideoDurationAsync(string videoId, CancellationToken cancellationToken);
    /// <summary>
    /// Fetches the duration of an audio file from a URL.
    /// For YouTube URLs, delegates to FetchVideoDurationAsync.
    /// For direct audio files (.mp3, .wav etc.) returns null (duration should be manually provided).
    /// For SoundCloud/Bandcamp etc., returns null for now.
    /// </summary>
    Task<TimeSpan?> FetchAudioDurationAsync(string url, CancellationToken cancellationToken);
}
