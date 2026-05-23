using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries.GetQuizAttemptById;

public class QuizAttemptDetailDto
{
    public Guid Id { get; init; }
    public Guid QuizId { get; init; }
    public string QuizTitle { get; init; } = string.Empty;
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public bool IsPassed { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int AttemptNumber { get; init; }
    public int TotalQuestions { get; init; }
    public int CorrectAnswersCount { get; init; }
    public List<QuizAttemptQuestionDto> Questions { get; init; } = new();
}

public class QuizAttemptQuestionDto
{
    public Guid QuestionId { get; init; }
    public string Text { get; init; } = string.Empty;
    public int Order { get; init; }
    public List<QuizAttemptOptionDto> Options { get; init; } = new();

    // Student selection
    public Guid? SelectedOptionId { get; init; }
    public bool IsCorrect { get; init; }
}

public class QuizAttemptOptionDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
    public string? Explanation { get; init; }
}
