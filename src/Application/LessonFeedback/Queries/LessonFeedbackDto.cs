namespace AskAMuslimBackend.Application.LessonFeedback.Queries;

public class LessonFeedbackDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid LessonId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public DateTimeOffset Created { get; init; }
}
