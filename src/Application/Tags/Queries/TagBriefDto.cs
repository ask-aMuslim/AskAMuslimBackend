using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Tags.Queries;

public class TagBriefDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int TotalCourses { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Tag, TagBriefDto>()
                .ForMember(dest => dest.TotalCourses, opt => opt.MapFrom(src => src.CourseTags.Count));
        }
    }
}
