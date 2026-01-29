namespace AskAMuslimBackend.Domain.Entities;

public class QuizAttemptAnswer : BaseEntity
{
    public Guid QuizAttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid SelectedOptionId { get; set; }
    public bool IsCorrect { get; set; }

    // Navigation properties
    public QuizAttempt? QuizAttempt { get; set; }
    public Question? Question { get; set; }
    public Option? SelectedOption { get; set; }
}
