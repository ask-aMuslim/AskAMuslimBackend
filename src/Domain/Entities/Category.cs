namespace AskAMuslimBackend.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<Course> Courses { get; set; } = new HashSet<Course>();
}

