using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Courses.Commands.CreateCourse;

public record CreateCourseCommand : IRequest<Guid>
{
    public Guid LevelId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public int Order { get; init; }
    public List<Guid> TagIds { get; init; } = new();
}

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // Defensive existence checks
        var levelExists = await _context.Levels.AsNoTracking().AnyAsync(l => l.Id == request.LevelId, cancellationToken);
        if (!levelExists)
        {
            throw new ValidationException(new[] { new FluentValidation.Results.ValidationFailure("LevelId", "Level does not exist.") });
        }

        if (request.TagIds.Count > 0)
        {
            var existingTagIds = await _context.Tags.AsNoTracking().Where(t => request.TagIds.Contains(t.Id)).Select(t => t.Id).ToListAsync(cancellationToken);
            var missing = request.TagIds.Except(existingTagIds).ToList();
            if (missing.Any())
            {
                throw new ValidationException(missing.Select(id => new FluentValidation.Results.ValidationFailure("TagIds", $"Tag {id} does not exist.")).ToList());
            }
        }

        var course = new Course
        {
            LevelId = request.LevelId,
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            IsPublished = request.IsPublished,
            Order = request.Order
        };

        if (request.TagIds.Any())
        {
            foreach (var tagId in request.TagIds.Distinct())
            {
                course.CourseTags.Add(new CourseTag { TagId = tagId, Course = course });
            }
        }

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
