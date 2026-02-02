using AskAMuslimBackend.Application.Tags.Queries;
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

    public List<TagBriefDto> Tags { get; init; } = new();

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Course, CourseBriefDto>()
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.CourseTags.Select(ct => ct.Tag)));
        }
    }
}
