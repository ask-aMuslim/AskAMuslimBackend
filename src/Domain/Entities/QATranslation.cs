namespace AskAMuslimBackend.Domain.Entities;

public class QATranslation : BaseAuditableEntity
{
    public Guid QAId { get; set; }
    public Language Language { get; set; } // e.g., en, ar
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;

    // Navigation
    public QA? QA { get; set; }
}
