using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Courses.Commands.AddCoursePrerequisite;

public record AddCoursePrerequisiteCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid CourseId { get; init; }
    public Guid PrerequisiteCourseId { get; init; }
    public int Order { get; init; }
}

public class AddCoursePrerequisiteCommandHandler : IRequestHandler<AddCoursePrerequisiteCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public AddCoursePrerequisiteCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(AddCoursePrerequisiteCommand request, CancellationToken cancellationToken)
    {
        // Check if both courses exist
        var courseExists = await _context.Courses
            .AnyAsync(c => c.Id == request.CourseId, cancellationToken);

        if (!courseExists)
        {
            return Result.Failure(new[] { _localizer["Courses.NotFound"] });
        }

        var prerequisiteExists = await _context.Courses
            .AnyAsync(c => c.Id == request.PrerequisiteCourseId, cancellationToken);

        if (!prerequisiteExists)
        {
            return Result.Failure(new[] { _localizer["Courses.PrerequisiteCourseNotFound"] });
        }

        // Check if courses are the same
        if (request.CourseId == request.PrerequisiteCourseId)
        {
            return Result.Failure(new[] { _localizer["Courses.SelfPrerequisite"] });
        }

        // Check if prerequisite already exists
        var existingPrerequisite = await _context.CoursePrerequisites
            .AnyAsync(cp => cp.CourseId == request.CourseId && 
                           cp.PrerequisiteCourseId == request.PrerequisiteCourseId, 
                      cancellationToken);

        if (existingPrerequisite)
        {
            return Result.Failure(new[] { _localizer["Courses.PrerequisiteAlreadyExists"] });
        }

        // Check for circular dependency
        if (await HasCircularDependency(request.CourseId, request.PrerequisiteCourseId, cancellationToken))
        {
            return Result.Failure(new[] { _localizer["Courses.PrerequisiteCircularDependency"] });
        }

        // Add the prerequisite
        var coursePrerequisite = new CoursePrerequisite
        {
            CourseId = request.CourseId,
            PrerequisiteCourseId = request.PrerequisiteCourseId,
            Order = request.Order
        };

        _context.CoursePrerequisites.Add(coursePrerequisite);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Checks if adding prerequisiteCourseId as a prerequisite to courseId would create a circular dependency.
    /// Uses BFS to detect if courseId is reachable from prerequisiteCourseId through the prerequisite chain.
    /// </summary>
    private async Task<bool> HasCircularDependency(Guid courseId, Guid prerequisiteCourseId, CancellationToken cancellationToken)
    {
        // If prerequisiteCourseId has courseId in its prerequisite chain (directly or indirectly),
        // then adding courseId -> prerequisiteCourseId would create a cycle.
        
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(prerequisiteCourseId);

        while (queue.Count > 0)
        {
            var currentCourseId = queue.Dequeue();

            if (currentCourseId == courseId)
            {
                // Found a path from prerequisiteCourseId to courseId
                return true;
            }

            if (!visited.Add(currentCourseId))
            {
                // Already visited this course
                continue;
            }

            // Get all prerequisites of the current course
            var prerequisites = await _context.CoursePrerequisites
                .Where(cp => cp.CourseId == currentCourseId)
                .Select(cp => cp.PrerequisiteCourseId)
                .ToListAsync(cancellationToken);

            foreach (var prereq in prerequisites)
            {
                queue.Enqueue(prereq);
            }
        }

        return false;
    }
}
