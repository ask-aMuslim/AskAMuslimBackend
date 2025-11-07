namespace AskAMuslimBackend.Domain.Entities;

public class StudentNote : BaseAuditableEntity
{
    // Changed StudentId to string to match Identity ApplicationUser Id
    public string StudentId { get; set; } = string.Empty;
    public Guid LessonId { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Timestamp { get; set; }

    // Navigation
    public Lesson? Lesson { get; set; }
}
