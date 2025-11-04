namespace AskAMuslimBackend.Domain.Entities;

public class Certificate : BaseAuditableEntity
{
    public int StudentId { get; set; }
    public int LevelId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Level? Level { get; set; }
}
