using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.Lessons.Commands.DeleteLesson;

public record DeleteLessonCommand(Guid Id) : IRequest;

public class DeleteLessonCommandHandler : IRequestHandler<DeleteLessonCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteLessonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Lessons.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);
        _context.Lessons.Remove(entity!);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
public class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Lesson Id is required.");
    }
}
