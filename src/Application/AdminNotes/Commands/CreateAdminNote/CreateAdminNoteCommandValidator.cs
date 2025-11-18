namespace AskAMuslimBackend.Application.AdminNotes.Commands.CreateAdminNote;

public class CreateAdminNoteCommandValidator : AbstractValidator<CreateAdminNoteCommand>
{
    public CreateAdminNoteCommandValidator()
    {
        RuleFor(v => v.StudentId)
            .NotEmpty();

        RuleFor(v => v.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
