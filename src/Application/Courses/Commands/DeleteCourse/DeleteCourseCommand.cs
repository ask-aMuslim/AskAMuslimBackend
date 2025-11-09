using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Courses.Commands.DeleteCourse;

public record DeleteCourseCommand(Guid Id) : IRequest;

public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Courses.FindAsync(new object[] { request.Id }, cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.Courses.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
