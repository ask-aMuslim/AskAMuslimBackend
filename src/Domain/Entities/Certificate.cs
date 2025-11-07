namespace AskAMuslimBackend.Domain.Entities;

public class Certificate : BaseAuditableEntity
{
    // Changed StudentId to string to match Identity ApplicationUser Id
    public string StudentId { get; set; } = string.Empty;
    public Guid LevelId { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Level? Level { get; set; }
}
