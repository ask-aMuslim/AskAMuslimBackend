namespace AskAMuslimBackend.Application.Preachers.Commands.UpdatePreacherProfile;

public class UpdatePreacherProfileCommandValidator : AbstractValidator<UpdatePreacherProfileCommand>
{
    public UpdatePreacherProfileCommandValidator()
    {
        RuleFor(v => v.PhoneNumber)
            .MaximumLength(20)
            .When(v => !string.IsNullOrEmpty(v.PhoneNumber));

        RuleFor(v => v.Bio)
            .MaximumLength(2000)
            .When(v => !string.IsNullOrEmpty(v.Bio));

        RuleFor(v => v.Specialization)
            .MaximumLength(200)
            .When(v => !string.IsNullOrEmpty(v.Specialization));

        RuleFor(v => v.Website)
            .MaximumLength(500)
            .When(v => !string.IsNullOrEmpty(v.Website));

        RuleFor(v => v.LinkedInProfile)
            .MaximumLength(500)
            .When(v => !string.IsNullOrEmpty(v.LinkedInProfile));

        RuleFor(v => v.YearsOfExperience)
            .InclusiveBetween(0, 100)
            .When(v => v.YearsOfExperience.HasValue);

        RuleFor(v => v.Certifications)
            .MaximumLength(1000)
            .When(v => !string.IsNullOrEmpty(v.Certifications));

        RuleFor(v => v.Organization)
            .MaximumLength(200)
            .When(v => !string.IsNullOrEmpty(v.Organization));
    }
}
