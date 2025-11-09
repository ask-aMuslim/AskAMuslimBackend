using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Categories.Queries;

public class CategoryBriefDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int TotalCourses { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Category, CategoryBriefDto>()
                .ForMember(dest => dest.TotalCourses, opt => opt.MapFrom(src => src.Courses.Count));
        }
    }
}
