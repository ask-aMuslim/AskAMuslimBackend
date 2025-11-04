namespace AskAMuslimBackend.Domain.Entities;

public class Question : BaseAuditableEntity
{
    public int QuizId { get; set; }
    public string Text { get; set; } = string.Empty;

    // Navigation
    public Quiz? Quiz { get; set; }
    public ICollection<Option> Options { get; set; } = new HashSet<Option>();
}
