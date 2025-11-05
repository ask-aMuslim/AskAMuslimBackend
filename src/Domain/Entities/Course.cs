namespace AskAMuslimBackend.Domain.Entities;

public class Course : BaseAuditableEntity
{
    public Guid LevelId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; }
    public double Rating { get; set; }

    // Navigation
    public Level? Level { get; set; }
    public Category? Category { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();
    public ICollection<Quiz> Quizzes { get; set; } = new HashSet<Quiz>();
    public ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();
}

