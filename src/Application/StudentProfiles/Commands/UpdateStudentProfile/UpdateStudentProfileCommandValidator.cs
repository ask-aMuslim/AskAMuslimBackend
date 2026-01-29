namespace AskAMuslimBackend.Application.StudentProfiles.Commands.UpdateStudentProfile;

public class UpdateStudentProfileCommandValidator : AbstractValidator<UpdateStudentProfileCommand>
{
    public UpdateStudentProfileCommandValidator()
    {
        RuleFor(v => v.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.")
            .When(v => !string.IsNullOrEmpty(v.FirstName));

        RuleFor(v => v.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.")
            .When(v => !string.IsNullOrEmpty(v.LastName));

        RuleFor(v => v.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .When(v => !string.IsNullOrEmpty(v.ImageUrl));

        RuleFor(v => v.PhoneNumber)
            .MaximumLength(20)
            .When(v => !string.IsNullOrEmpty(v.PhoneNumber))
            .WithMessage("Phone number must not exceed 20 characters");

        RuleFor(v => v.Country)
            .MaximumLength(100)
            .When(v => !string.IsNullOrEmpty(v.Country))
            .WithMessage("Country must not exceed 100 characters");

        RuleFor(v => v.CountryCode)
            .MaximumLength(10)
            .When(v => !string.IsNullOrEmpty(v.CountryCode))
            .WithMessage("Country code must not exceed 10 characters");

        RuleFor(v => v.Address)
            .MaximumLength(500)
            .When(v => !string.IsNullOrEmpty(v.Address))
            .WithMessage("Address must not exceed 500 characters");

        RuleFor(v => v.Bio)
            .MaximumLength(1000)
            .When(v => !string.IsNullOrEmpty(v.Bio))
            .WithMessage("Bio must not exceed 1000 characters");

        RuleFor(v => v.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(v => v.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be in the past");

        RuleFor(v => v.DateOfIslamConversion)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .When(v => v.DateOfIslamConversion.HasValue)
            .WithMessage("Date of Islam conversion must not be in the future");

        RuleFor(v => v.OldReligion)
            .MaximumLength(100)
            .When(v => !string.IsNullOrEmpty(v.OldReligion))
            .WithMessage("Old religion must not exceed 100 characters");

        RuleFor(v => v.ReasonForConversion)
            .MaximumLength(2000)
            .When(v => !string.IsNullOrEmpty(v.ReasonForConversion))
            .WithMessage("Reason for conversion must not exceed 2000 characters");
    }
}
