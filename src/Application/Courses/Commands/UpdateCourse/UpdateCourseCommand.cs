using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Courses.Commands.UpdateCourse;

public record UpdateCourseCommand : IRequest
{
    public Guid Id { get; init; }
    public Guid LevelId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public int Order { get; init; }
    public List<Guid> TagIds { get; init; } = new();
}

public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Courses
            .Include(c => c.CourseTags)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.LevelId = request.LevelId;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.ThumbnailUrl = request.ThumbnailUrl;
        entity.IsPublished = request.IsPublished;
        entity.Order = request.Order;

        // update tags
        var current = entity.CourseTags.Select(ct => ct.TagId).ToHashSet();
        var desired = request.TagIds.Distinct().ToHashSet();

        var toRemove = entity.CourseTags.Where(ct => !desired.Contains(ct.TagId)).ToList();
        foreach (var rem in toRemove)
            entity.CourseTags.Remove(rem);

        var toAdd = desired.Except(current);
        foreach (var tagId in toAdd)
            entity.CourseTags.Add(new CourseTag { TagId = tagId, CourseId = entity.Id });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
