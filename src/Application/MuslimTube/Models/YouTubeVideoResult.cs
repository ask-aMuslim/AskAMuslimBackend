namespace AskAMuslimBackend.Application.MuslimTube.Models;

public record YouTubeVideoResult(
    string YouTubeVideoId,
    string Title,
    string? Description,
    string? ThumbnailDefaultUrl,
    string? ThumbnailMediumUrl,
    string? ThumbnailHighUrl,
    DateTimeOffset? PublishedAt
);
