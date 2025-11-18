namespace AskAMuslimBackend.Application.StudentQuestions.Queries.GetStudentQuestions;

public class StudentQuestionDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public bool IsAnswered { get; set; }
    public DateTimeOffset? AnsweredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
