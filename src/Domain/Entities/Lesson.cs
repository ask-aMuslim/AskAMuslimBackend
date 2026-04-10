using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class Lesson : BaseAuditableEntity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentJson { get; set; }
    public LessonType Type { get; set; }
    public string? ContentUrl { get; set; }
    public string? Transcript { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int Order { get; set; }    public bool IsPublished { get; set; } = false;
    public bool HasQuiz { get; set; } = false;
    public Language Language { get; set; } = Language.English;
    // Navigation
    public Course? Course { get; set; }
    public ICollection<StudentQuestion> StudentQuestions { get; set; } = new HashSet<StudentQuestion>();
    public ICollection<StudentNote> StudentNotes { get; set; } = new HashSet<StudentNote>();
}
