using AskAMuslimBackend.Application.Common.Interfaces;
using FluentValidation;

namespace AskAMuslimBackend.Application.StudentNotes.Commands.DeleteStudentNote;

public record DeleteStudentNoteCommand(Guid Id) : IRequest;

public class DeleteStudentNoteCommandHandler : IRequestHandler<DeleteStudentNoteCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteStudentNoteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteStudentNoteCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentNotes.FindAsync(new object[] { request.Id }, cancellationToken);
        Guard.Against.NotFound(request.Id, entity);

        _context.StudentNotes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteStudentNoteCommandValidator : AbstractValidator<DeleteStudentNoteCommand>
{
    public DeleteStudentNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
