using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Courses.Commands.CreateCourse;

public record CreateCourseCommand : IRequest<Guid>
{
    public Guid CategoryId { get; init; }
    public Guid LevelId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public bool IsPublished { get; init; }
    public int Order { get; init; }
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
        // Defensive existence checks (in addition to FluentValidation) to avoid FK violation surfacing as DbUpdateException.
        var categoryExists = await _context.Categories.AsNoTracking().AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        var levelExists = await _context.Levels.AsNoTracking().AnyAsync(l => l.Id == request.LevelId, cancellationToken);

        if (!categoryExists || !levelExists)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            if (!categoryExists)
                failures.Add(new("CategoryId", "Category does not exist."));
            if (!levelExists)
                failures.Add(new("LevelId", "Level does not exist."));
            throw new ValidationException(failures);
        }

        var course = new Course
        {
            CategoryId = request.CategoryId,
            LevelId = request.LevelId,
            Title = request.Title,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            IsPublished = request.IsPublished,
            Order = request.Order
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        return course.Id;
    }
}
