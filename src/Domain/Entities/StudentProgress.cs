namespace AskAMuslimBackend.Domain.Entities;

public class StudentProgress : BaseAuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid LessonId { get; set; }
    public bool IsCompleted { get; set; }
    public double? ProgressPercentage { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public Student? Student { get; set; }
    public Course? Course { get; set; }
    public Lesson? Lesson { get; set; }
}