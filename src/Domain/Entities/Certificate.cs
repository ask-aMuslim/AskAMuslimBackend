namespace AskAMuslimBackend.Domain.Entities;

public class Certificate : BaseAuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid LevelId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Level? Level { get; set; }
}
