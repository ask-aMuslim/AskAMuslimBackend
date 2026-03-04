using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.Courses.Commands.RemoveCoursePrerequisite;

public record RemoveCoursePrerequisiteCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid CourseId { get; init; }
    [JsonIgnore] // Set from route parameter, not request body
    public Guid PrerequisiteCourseId { get; init; }
}

public class RemoveCoursePrerequisiteCommandHandler : IRequestHandler<RemoveCoursePrerequisiteCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public RemoveCoursePrerequisiteCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(RemoveCoursePrerequisiteCommand request, CancellationToken cancellationToken)
    {
        var prerequisite = await _context.CoursePrerequisites
            .FirstOrDefaultAsync(cp => cp.CourseId == request.CourseId && 
                                      cp.PrerequisiteCourseId == request.PrerequisiteCourseId, 
                                cancellationToken);

        if (prerequisite == null)
        {
            return Result.Failure(new[] { _localizer["Courses.PrerequisiteNotFound"] });
        }

        _context.CoursePrerequisites.Remove(prerequisite);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
