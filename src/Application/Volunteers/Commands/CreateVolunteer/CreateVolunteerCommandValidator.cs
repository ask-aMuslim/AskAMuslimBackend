using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace AskAMuslimBackend.Application.Volunteers.Commands.CreateVolunteer;

public class CreateVolunteerCommandValidator : AbstractValidator<CreateVolunteerCommand>
{
    private static readonly string[] DocumentExtensions = { ".pdf", ".doc", ".docx" };
    private static readonly string[] DocumentContentTypes = {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private const long MaxDocumentSize = 10 * 1024 * 1024; // 10MB

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

        When(v => v.CvFile != null, () =>
        {
            RuleFor(v => v.CvFile)
                .NotNull().WithMessage("CV file is required.")
                .Must(HaveValidExtension).WithMessage($"CV must be one of: {string.Join(", ", DocumentExtensions)}")
                .Must(HaveValidContentType).WithMessage("CV must have a valid content type.")
                .Must(BeWithinSizeLimit).WithMessage($"CV file size must not exceed {MaxDocumentSize / 1024 / 1024}MB.");
        });
    }

    private bool HaveValidExtension(IFormFile? file)
    {
        if (file == null) return false;
        var extension = Path.GetExtension(file.FileName).ToLower();
        return DocumentExtensions.Contains(extension);
    }

    private bool HaveValidContentType(IFormFile? file)
    {
        if (file == null) return false;
        return DocumentContentTypes.Contains(file.ContentType.ToLower());
    }

    private bool BeWithinSizeLimit(IFormFile? file)
    {
        if (file == null) return false;
        return file.Length <= MaxDocumentSize;
    }
}
