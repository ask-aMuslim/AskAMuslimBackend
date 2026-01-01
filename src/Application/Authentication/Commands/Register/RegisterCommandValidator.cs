using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Authentication.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid role selected.")
            .NotEqual(UserRole.Admin).WithMessage("Cannot register as admin.");
    }

    private bool BeAValidImage(IFormFile? file)
    {
        if (file == null) return true;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(extension);
    }

    private bool BeAValidSize(IFormFile? file)
    {
        if (file == null) return true;

        const long maxSize = 5 * 1024 * 1024; // 5MB
        return file.Length <= maxSize;
    }
}
