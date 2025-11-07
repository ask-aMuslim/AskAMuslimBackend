namespace AskAMuslimBackend.Domain.Entities;

public class Enrollment : BaseAuditableEntity
{
    // Changed StudentId to string to match Identity ApplicationUser Id
    public string StudentId { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public double LevelProgress { get; set; }
    public double CourseProgress { get; set; }

    // Navigation
    public Course? Course { get; set; }
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new HashSet<QuizAttempt>();
}

