using FluentValidation;

namespace AskAMuslimBackend.Application.Volunteers.Commands.CreateVolunteer;

public class CreateVolunteerCommandValidator : AbstractValidator<CreateVolunteerCommand>
{
    public CreateVolunteerCommandValidator()
    {
        RuleFor(v => v.Name)
            .MaximumLength(200)
            .NotEmpty();

        RuleFor(v => v.Email)
            .MaximumLength(200)
            .NotEmpty()
            .EmailAddress();
            
        RuleFor(v => v.Phone)
            .MaximumLength(50);
            
        RuleFor(v => v.Message)
            .MaximumLength(2000);
            
        RuleFor(v => v.Type)
            .IsInEnum();
    }
}