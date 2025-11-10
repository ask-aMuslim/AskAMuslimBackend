namespace AskAMuslimBackend.Domain.Entities;

public class CourseTag
{
    public Guid CourseId { get; set; }
    public Guid TagId { get; set; }

    // Navigation
    public Course? Course { get; set; }
    public Tag? Tag { get; set; }
}
