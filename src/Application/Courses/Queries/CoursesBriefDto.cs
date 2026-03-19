using AskAMuslimBackend.Application.Tags.Queries;
using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Courses.Queries;

public class CourseBriefDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? ThumbnailUrl { get; init; }

    public bool IsPublished { get; init; }

    public double Rating { get; init; }

    public int Order { get; init; }

    public Language Language { get; init; }

    public List<TagBriefDto> Tags { get; init; } = new();
}
