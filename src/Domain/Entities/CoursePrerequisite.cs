namespace AskAMuslimBackend.Domain.Entities;

/// <summary>
/// Join entity for many-to-many self-referencing relationship between courses.
/// Represents prerequisite requirements: CourseId requires PrerequisiteCourseId to be completed first.
/// </summary>
public class CoursePrerequisite
{
    /// <summary>
    /// The course that has the prerequisite requirement.
    /// </summary>
    public Guid CourseId { get; set; }

    /// <summary>
    /// The course that must be completed before enrolling in CourseId.
    /// </summary>
    public Guid PrerequisiteCourseId { get; set; }

    /// <summary>
    /// Optional order/priority for displaying prerequisites (lower values shown first).
    /// </summary>
    public int Order { get; set; }

    // Navigation properties
    public Course? Course { get; set; }
    public Course? PrerequisiteCourse { get; set; }
}
