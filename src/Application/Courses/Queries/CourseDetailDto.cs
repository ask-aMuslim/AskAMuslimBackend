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

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Course, CourseDetailDto>()
                .ForMember(d => d.Prerequisites, opt => opt.MapFrom(src => 
                    src.CoursePrerequisites
                        .OrderBy(cp => cp.Order)
                        .Select(cp => new PrerequisiteCourseDto
                        {
                            Id = cp.PrerequisiteCourseId,
                            Title = cp.PrerequisiteCourse!.Title,
                            Order = cp.Order
                        })))
                .ForMember(d => d.UnlockedCourses, opt => opt.MapFrom(src => 
                    src.PrerequisiteForCourses
                        .Select(cp => new PrerequisiteCourseDto
                        {
                            Id = cp.CourseId,
                            Title = cp.Course!.Title,
                            Order = cp.Order
                        })));
        }
    }
}

public class PrerequisiteCourseDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int Order { get; init; }
    public bool IsCompleted { get; set; }
}
