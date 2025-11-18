namespace AskAMuslimBackend.Domain.Entities;

public class MuslimTubeVideo : BaseAuditableEntity
{
    public string YouTubeVideoId { get; set; } = string.Empty;
    public Guid MuslimTubeChannelId { get; set; }
    public MuslimTubeChannel? Channel { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Thumbnails
    public string? ThumbnailDefaultUrl { get; set; }
    public string? ThumbnailMediumUrl { get; set; }
    public string? ThumbnailHighUrl { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    // Direct link to watch the video on YouTube
    public string VideoUrl { get; set; } = string.Empty;
}
