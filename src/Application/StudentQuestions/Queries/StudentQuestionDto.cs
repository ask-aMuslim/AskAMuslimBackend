using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.StudentQuestions.Queries;

public class StudentQuestionDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid LessonId { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public double Timestamp { get; init; }
    public bool Answered { get; init; }
    public string? AnswerText { get; init; }
}
