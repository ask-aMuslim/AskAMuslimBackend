namespace AskAMuslimBackend.Domain.Entities;

public class Option : BaseAuditableEntity
{
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;

    // Navigation
    public Question? Question { get; set; }
}
