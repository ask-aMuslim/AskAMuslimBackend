using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class Level : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public  int Order { get; set; }
    public LevelDifficulty Difficulty { get; set; }    public bool IsPublished { get; set; } = false;
    public Language Language { get; set; } = Language.English;
    // Navigation
    public ICollection<Course> Courses { get; set; } = new HashSet<Course>();

    // Each level has at most one certificate
    public Certificate? Certificate { get; set; }
}
