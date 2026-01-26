using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Courses.Commands.RemoveCoursePrerequisite;

public record RemoveCoursePrerequisiteCommand : IRequest<Result>
{
    public Guid CourseId { get; init; }
    public Guid PrerequisiteCourseId { get; init; }
}

public class RemoveCoursePrerequisiteCommandHandler : IRequestHandler<RemoveCoursePrerequisiteCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public RemoveCoursePrerequisiteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RemoveCoursePrerequisiteCommand request, CancellationToken cancellationToken)
    {
        var prerequisite = await _context.CoursePrerequisites
            .FirstOrDefaultAsync(cp => cp.CourseId == request.CourseId && 
                                      cp.PrerequisiteCourseId == request.PrerequisiteCourseId, 
                                cancellationToken);

        if (prerequisite == null)
        {
            return Result.Failure(new[] { "Prerequisite relationship not found." });
        }

        _context.CoursePrerequisites.Remove(prerequisite);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
