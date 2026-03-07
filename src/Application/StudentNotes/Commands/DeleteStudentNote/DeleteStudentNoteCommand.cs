using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.StudentNotes.Commands.DeleteStudentNote;

public record DeleteStudentNoteCommand([property: JsonIgnore] Guid Id) : IRequest<Result>;

public class DeleteStudentNoteCommandHandler : IRequestHandler<DeleteStudentNoteCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public DeleteStudentNoteCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(DeleteStudentNoteCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentNotes.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (entity == null)
            return Result.Failure(new[] { _localizer["StudentNotes.NotFound"] });

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
