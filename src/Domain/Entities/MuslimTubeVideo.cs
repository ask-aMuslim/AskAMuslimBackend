using AskAMuslimBackend.Domain.Common;
using System;
using AskAMuslimBackend.Domain.Enums;

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

    // Updated default: generic long-form video
    public MuslimTubeVideoType Type { get; set; } = MuslimTubeVideoType.Video;
}
