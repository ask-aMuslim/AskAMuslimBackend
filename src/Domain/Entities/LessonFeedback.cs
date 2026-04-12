namespace AskAMuslimBackend.Domain.Entities;

public class LessonFeedback : BaseAuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid LessonId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Navigation
    public Student? Student { get; set; }
    public Lesson? Lesson { get; set; }
}
