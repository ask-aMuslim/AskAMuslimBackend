using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Tags.Queries;

public class TagBriefDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Order { get; init; }
    public int TotalCourses { get; init; }
    public int TotalQAs { get; init; }
}
