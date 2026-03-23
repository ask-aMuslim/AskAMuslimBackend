using FluentValidation;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Commands.CreateLearnIslamRequest;

public class CreateLearnIslamRequestCommandValidator : AbstractValidator<CreateLearnIslamRequestCommand>
{
    public CreateLearnIslamRequestCommandValidator()
    {
        RuleFor(v => v.FullName)
            .MaximumLength(200)
            .NotEmpty();

        RuleFor(v => v.Email)
            .MaximumLength(200)
            .NotEmpty()
            .EmailAddress();
            
        RuleFor(v => v.Country)
            .MaximumLength(100)
            .NotEmpty();
            
        RuleFor(v => v.Message)
            .MaximumLength(2000);
            
        RuleFor(v => v.CurrentReligion)
            .MaximumLength(100);
            
        RuleFor(v => v.PreferredLanguage)
            .IsInEnum();
            
        RuleFor(v => v.KnowledgeLevel)
            .IsInEnum();
    }
}