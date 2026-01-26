namespace AskAMuslimBackend.Domain.Entities;

public class Course : BaseAuditableEntity
{
    public Guid LevelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; }
    public double Rating { get; set; }
    public int Order { get; set; }

    // Navigation
    public Level? Level { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new HashSet<Lesson>();
    public ICollection<Quiz> Quizzes { get; set; } = new HashSet<Quiz>();
    public ICollection<Enrollment> Enrollments { get; set; } = new HashSet<Enrollment>();

    // Tags (many-to-many via join entity)
    public ICollection<CourseTag> CourseTags { get; set; } = new HashSet<CourseTag>();

    // Prerequisites (many-to-many self-referencing via join entity)
    /// <summary>
    /// Join entities representing courses that must be completed before this course.
    /// </summary>
    public ICollection<CoursePrerequisite> CoursePrerequisites { get; set; } = new HashSet<CoursePrerequisite>();

    /// <summary>
    /// Join entities representing courses that require this course as a prerequisite.
    /// </summary>
    public ICollection<CoursePrerequisite> PrerequisiteForCourses { get; set; } = new HashSet<CoursePrerequisite>();
}

