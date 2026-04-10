namespace AskAMuslimBackend.Domain.Entities;

public class Enrollment : BaseAuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public double LevelProgress { get; set; }
    
    // Aggregation for course progress tracking
    public int CompletedLessonsCount { get; set; } = 0;
    public int TotalLessonsCount { get; set; } = 0;
    public double CourseProgress => TotalLessonsCount == 0 ? 0 : Math.Round(((double)CompletedLessonsCount / TotalLessonsCount) * 100, 2);
    public DateTime? LastUpdatedAt { get; set; }

    // Navigation
    public Student? Student { get; set; }
    public Course? Course { get; set; }
}

