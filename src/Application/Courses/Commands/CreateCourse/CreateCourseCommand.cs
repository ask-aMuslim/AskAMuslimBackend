using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Courses.Commands.CreateCourse;

public record CreateCourseCommand : IRequest<Result<Guid>>
{
    public Guid LevelId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public int Order { get; init; }
    public Language Language { get; init; } = Language.English;
    public List<Guid> TagIds { get; init; } = new();
}

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // Assumes validator already ensured LevelId and TagIds exist.
        var course = new Course
        {
            LevelId = request.LevelId,
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            IsPublished = request.IsPublished,
            Order = request.Order,
            Language = request.Language
        };

        if (request.TagIds.Count != 0)
        {
            foreach (var tagId in request.TagIds.Distinct())
            {
                course.CourseTags.Add(new CourseTag { TagId = tagId, Course = course });
            }
        }

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(course.Id);
    }
}
