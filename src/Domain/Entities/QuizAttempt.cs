namespace AskAMuslimBackend.Domain.Entities;

public class QuizAttempt : BaseAuditableEntity
{
    public Guid QuizId { get; set; }
    public Guid EnrollmentId { get; set; }
    public double Score { get; set; }
    public bool IsPassed { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int AttemptNumber { get; set; }

    // Navigation
    public Quiz? Quiz { get; set; }
    public Enrollment? Enrollment { get; set; }
}
