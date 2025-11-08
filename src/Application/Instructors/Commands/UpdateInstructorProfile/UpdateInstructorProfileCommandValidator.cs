using FluentValidation;

namespace AskAMuslimBackend.Application.Instructors.Commands.UpdateInstructorProfile;

public class UpdateInstructorProfileCommandValidator : AbstractValidator<UpdateInstructorProfileCommand>
{
    public UpdateInstructorProfileCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Bio));

        RuleFor(x => x.Specialization)
            .MaximumLength(200).WithMessage("Specialization must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Specialization));

        RuleFor(x => x.Website)
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.LinkedInProfile)
            .MaximumLength(500).WithMessage("LinkedIn profile must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.LinkedInProfile));

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be 0 or greater.")
            .LessThanOrEqualTo(100).WithMessage("Years of experience must be 100 or less.")
            .When(x => x.YearsOfExperience.HasValue);
    }
}
