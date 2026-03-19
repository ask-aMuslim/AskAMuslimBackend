using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Courses.Queries;

public class CourseDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public double Rating { get; init; }
    public int Order { get; init; }
    public Guid LevelId { get; init; }

    public List<PrerequisiteCourseDto> Prerequisites { get; init; } = new();
    public List<PrerequisiteCourseDto> UnlockedCourses { get; init; } = new();
}

public class PrerequisiteCourseDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int Order { get; init; }
    public bool IsCompleted { get; set; }
}
