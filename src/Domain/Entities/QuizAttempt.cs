namespace AskAMuslimBackend.Domain.Entities;

public class QuizAttempt : BaseAuditableEntity
{
    public Guid QuizId { get; set; }
    public Guid StudentId { get; set; }
    public decimal Score { get; set; } 
    public bool IsPassed { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int AttemptNumber { get; set; }

    // Navigation
    public Quiz? Quiz { get; set; }
    public Student? Student { get; set; }
}
