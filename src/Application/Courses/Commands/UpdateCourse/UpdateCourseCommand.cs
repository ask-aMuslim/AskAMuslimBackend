using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Courses.Commands.UpdateCourse;

public record UpdateCourseCommand : IRequest
{
    public Guid Id { get; init; }
    public Guid CategoryId { get; init; }
    public Guid LevelId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public int Order { get; init; }
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
        var entity = await _context.Courses.FindAsync(new object[] { request.Id }, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.CategoryId = request.CategoryId;
        entity.LevelId = request.LevelId;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.ThumbnailUrl = request.ThumbnailUrl;
        entity.IsPublished = request.IsPublished;
        entity.Order = request.Order;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
