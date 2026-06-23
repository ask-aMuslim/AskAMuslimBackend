namespace AskAMuslimBackend.Domain.Entities;

public class QA : BaseAuditableEntity
{
    public bool IsPublished { get; set; }
    public int Order { get; set; }

    // Navigation
    public ICollection<QATranslation> Translations { get; set; } = new HashSet<QATranslation>();
    public ICollection<QATag> QATags { get; set; } = new HashSet<QATag>();
}
