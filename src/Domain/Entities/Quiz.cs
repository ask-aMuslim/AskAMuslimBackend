namespace AskAMuslimBackend.Domain.Entities;

public class Quiz : BaseAuditableEntity
{
    // Replaced TargetId with specific hierarchical identifiers
    public Guid? LevelId { get; set; }
    public Guid? CourseId { get; set; }
    public Guid? LessonId { get; set; }
    public QuizTargetType TargetType { get; set; }
    public string Title { get; set; } = string.Empty;
    public double TotalMarks { get; set; }

    // Navigation
    public Level? Level { get; set; }
    public Course? Course { get; set; }
    public Lesson? Lesson { get; set; }
    public ICollection<Question> Questions { get; set; } = new HashSet<Question>();
}
