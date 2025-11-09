using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Lessons.Queries;

public class LessonBriefDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public LessonType Type { get; init; }
    public int Order { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? ContentUrl { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Lesson, LessonBriefDto>();
        }
    }
}
