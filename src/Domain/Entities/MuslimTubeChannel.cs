namespace AskAMuslimBackend.Domain.Entities;

public class MuslimTubeChannel : BaseAuditableEntity
{
    public string YouTubeChannelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Thumbnails
    public string? ThumbnailDefaultUrl { get; set; }
    public string? ThumbnailMediumUrl { get; set; }
    public string? ThumbnailHighUrl { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSyncedAt { get; set; }

    public bool IsDeleted { get; set; } // Soft delete flag

    public ICollection<MuslimTubeVideo> Videos { get; set; } = new List<MuslimTubeVideo>();
}
