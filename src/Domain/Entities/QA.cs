namespace AskAMuslimBackend.Domain.Entities;

public class QA : BaseAuditableEntity
{
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsPublished { get; set; }

    // Navigation
    public ICollection<QATranslation> Translations { get; set; } = new HashSet<QATranslation>();
    public ICollection<QATag> QATags { get; set; } = new HashSet<QATag>();
}
