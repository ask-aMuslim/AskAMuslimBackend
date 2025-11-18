namespace AskAMuslimBackend.Application.MuslimTube.Models;

public record YouTubeChannelResult(
    string YouTubeChannelId,
    string Name,
    string? Description,
    string? ThumbnailDefaultUrl,
    string? ThumbnailMediumUrl,
    string? ThumbnailHighUrl
);
