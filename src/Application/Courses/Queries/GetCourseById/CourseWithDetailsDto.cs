using AskAMuslimBackend.Application.Courses.Queries;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCourseById;

public class CourseWithDetailsDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public bool HasQuiz { get; init; }
    public double Rating { get; init; }
    public int Order { get; init; }
    public Language Language { get; init; }
    public Guid LevelId { get; init; }
    public string? LevelName { get; init; }
    public int LessonsCount { get; init; }
    public TimeSpan? CourseDuration { get; init; }
    public bool IsCompleted { get; set; }
    public double CourseProgress { get; set; }

    public List<Tags.Queries.TagBriefDto> Tags { get; init; } = new();
    public List<CourseLessonDto> Lessons { get; init; } = new();
    public List<PrerequisiteCourseDto> Prerequisites { get; init; } = new();
    public List<PrerequisiteCourseDto> UnlockedCourses { get; init; } = new();
}

public class CourseLessonDto
{
    public Guid LessonId { get; init; }
    public string LessonName { get; init; } = string.Empty;
    public LessonType LessonType { get; init; }
    public TimeSpan? LessonDuration { get; init; }
    public bool IsPublished { get; init; }
    public bool LessonHasQuiz { get; init; }
    public bool IsLessonQuizPassed { get; set; }
    public bool IsLessonCompleted { get; set; }
    public string? QuizName { get; set; }
}
