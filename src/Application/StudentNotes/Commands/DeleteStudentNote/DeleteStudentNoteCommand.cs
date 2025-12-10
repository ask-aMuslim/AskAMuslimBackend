using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;

namespace AskAMuslimBackend.Application.StudentNotes.Commands.DeleteStudentNote;

public record DeleteStudentNoteCommand(Guid Id) : IRequest<Result>;

public class DeleteStudentNoteCommandHandler : IRequestHandler<DeleteStudentNoteCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteStudentNoteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(DeleteStudentNoteCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentNotes.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { "Student note not found." });

        _context.StudentNotes.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class DeleteStudentNoteCommandValidator : AbstractValidator<DeleteStudentNoteCommand>
{
    public DeleteStudentNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
