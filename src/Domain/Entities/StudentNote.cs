namespace AskAMuslimBackend.Domain.Entities;

public class StudentNote : BaseAuditableEntity
{
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Timestamp { get; set; }

    // Navigation
    public Lesson? Lesson { get; set; }
}
