namespace AskAMuslimBackend.Domain.Entities;

public class Tag : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<CourseTag> CourseTags { get; set; } = new HashSet<CourseTag>();
    public ICollection<QATag> QATags { get; set; } = new HashSet<QATag>();
}
