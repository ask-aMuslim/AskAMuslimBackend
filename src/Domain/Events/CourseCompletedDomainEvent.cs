namespace AskAMuslimBackend.Domain.Events;

public class CourseCompletedDomainEvent : BaseEvent
{
    public CourseCompletedDomainEvent(
        Guid enrollmentId,
        Guid studentId,
        Guid courseId,
        Guid levelId,
        DateTime completedAt)
    {
        EnrollmentId = enrollmentId;
        StudentId = studentId;
        CourseId = courseId;
        LevelId = levelId;
        CompletedAt = completedAt;
    }

    public Guid EnrollmentId { get; }
    public Guid StudentId { get; }
    public Guid CourseId { get; }
    public Guid LevelId { get; }
    public DateTime CompletedAt { get; }
}
