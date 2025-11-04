namespace AskAMuslimBackend.Domain.Entities;

public class Quiz : BaseAuditableEntity
{
    public int TargetId { get; set; }
    public QuizTargetType TargetType { get; set; }
    public string Title { get; set; } = string.Empty;
    public double TotalMarks { get; set; }

    // Navigation
    public ICollection<Question> Questions { get; set; } = new HashSet<Question>();
}
