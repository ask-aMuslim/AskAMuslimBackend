using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Levels.Queries;

public class LevelBriefDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Order { get; init; }
    public LevelDifficulty Difficulty { get; init; }
    public bool IsPublished { get; init; }
    public Language Language { get; init; }
    public int TotalCourses { get; init; }
    public bool HasQuiz { get; init; }
    public bool CanAccessLevel { get; set; } = true;
}
