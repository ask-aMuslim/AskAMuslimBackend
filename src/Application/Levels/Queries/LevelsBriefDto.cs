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
    public int TotalCourses { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Level, LevelBriefDto>()
                .ForMember(dest => dest.TotalCourses, opt => opt.MapFrom(src => src.Courses.Count));
        }
    }
}
