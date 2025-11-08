namespace AskAMuslimBackend.Domain.Entities;

public class StudentQuestion : BaseAuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid LessonId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public double Timestamp { get; set; } // seconds in the video
    public bool Answered { get; set; }
    public string? AnswerText { get; set; }

    // Navigation
    public Lesson? Lesson { get; set; }
}
