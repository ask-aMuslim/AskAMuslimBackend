using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.QuizAttempts.Queries;

public class QuizAttemptDto
{
    public Guid Id { get; init; }
    public Guid QuizId { get; init; }
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public decimal Score { get; init; }
    public bool IsPassed { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int AttemptNumber { get; init; }
}
