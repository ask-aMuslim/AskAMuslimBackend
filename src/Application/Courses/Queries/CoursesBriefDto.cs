using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Courses.Queries;

public class CourseBriefDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsPublished { get; init; }

    public double Rating { get; init; }

    public int Order { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Course, CourseBriefDto>();
        }
    }
}
