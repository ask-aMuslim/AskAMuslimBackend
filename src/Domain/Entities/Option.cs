namespace AskAMuslimBackend.Domain.Entities;

public class Option : BaseAuditableEntity
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    // Navigation
    public Question? Question { get; set; }
}
