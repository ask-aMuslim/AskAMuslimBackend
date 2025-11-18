using AskAMuslimBackend.Application.MuslimTube.Models;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IYouTubeService
{
    Task<YouTubeChannelResult?> FetchChannelAsync(string channelInput, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<YouTubeVideoResult>> FetchChannelVideosAsync(string youTubeChannelId, int maxResults, CancellationToken cancellationToken);
}
