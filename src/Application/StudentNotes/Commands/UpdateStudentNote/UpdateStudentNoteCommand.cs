using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Application.StudentNotes.Commands.UpdateStudentNote;

public record UpdateStudentNoteCommand : IRequest<Result>
{
    [JsonIgnore] // Set from route parameter, not request body
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public double Timestamp { get; init; }
}

public class UpdateStudentNoteCommandHandler : IRequestHandler<UpdateStudentNoteCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ILocalizationService _localizer;

    public UpdateStudentNoteCommandHandler(IApplicationDbContext context, ILocalizationService localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<Result> Handle(UpdateStudentNoteCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.StudentNotes.FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null)
            return Result.Failure(new[] { _localizer["StudentNotes.NotFound"] });

        entity.Text = request.Text;
        entity.Timestamp = request.Timestamp;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateStudentNoteCommandValidator : AbstractValidator<UpdateStudentNoteCommand>
{
    public UpdateStudentNoteCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(5000).WithMessage("Text must not exceed 5000 characters.");
        RuleFor(x => x.Timestamp).GreaterThanOrEqualTo(0);
    }
}