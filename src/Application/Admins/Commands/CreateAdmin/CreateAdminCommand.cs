using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;

namespace AskAMuslimBackend.Application.Admins.Commands.CreateAdmin;

[Authorize(Policy = Policies.ManageAdmins)]
public record CreateAdminCommand : IRequest<Result<Guid>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Role { get; init; } = Roles.Administrator; // Administrator, Instructor, or Preacher
    public string? Bio { get; init; }
    public string? ImageUrl { get; init; }
}

public class CreateAdminCommandValidator : AbstractValidator<CreateAdminCommand>
{
    public CreateAdminCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Valid email is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => role == Roles.Administrator || role == Roles.Instructor || role == Roles.Preacher)
            .WithMessage("Role must be Administrator, Instructor, or Preacher");
    }
}

public class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, Result<Guid>>
{
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;

    public CreateAdminCommandHandler(
        IIdentityService identityService,
        IApplicationDbContext context)
    {
        _identityService = identityService;
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        // Map role string to UserRole enum
        var userRole = (UserRole)Enum.Parse(typeof(UserRole), request.Role, true);

        // Use IdentityService to register the user
        // allowAdminCreation: true - This command is protected by Policies.ManageAdmins
        var result = await _identityService.RegisterUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            userRole,
            ReligiousStatus.BornMuslim, // Default to BornMuslim for admins
            null, // No image file in this command
            cancellationToken,
            allowAdminCreation: true); // ← Allow admin creation (authorized by policy)

        if (!result.Succeeded)
        {
            return Result<Guid>.Failure(result.Errors);
        }

        // Parse the userId from the response
        if (result.Data == null || !Guid.TryParse(result.Data.UserId, out Guid userId))
        {
            return Result<Guid>.Failure(new[] { "Failed to parse user ID" });
        }

        // Update additional fields if provided (Bio, ImageUrl, PhoneNumber)
        if (!string.IsNullOrWhiteSpace(request.Bio) || !string.IsNullOrWhiteSpace(request.ImageUrl) || !string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            await UpdateAdditionalFields(userId, request, userRole, cancellationToken);
        }

        return Result<Guid>.Success(userId);
    }

    private async Task UpdateAdditionalFields(Guid userId, CreateAdminCommand request, UserRole userRole, CancellationToken cancellationToken)
    {
        // Update user phone number if provided
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user != null)
            {
                user.PhoneNumber = request.PhoneNumber;
            }
        }

        // Update role-specific entity with Bio and ImageUrl
        if (userRole == UserRole.Admin)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
            if (admin != null)
            {
                admin.Bio = request.Bio;
                admin.ImageUrl = request.ImageUrl;
            }
        }
        else if (userRole == UserRole.Instructor)
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId, cancellationToken);
            if (instructor != null)
            {
                instructor.Bio = request.Bio;
                instructor.ImageUrl = request.ImageUrl;
            }
        }
        else if (userRole == UserRole.Preacher)
        {
            var preacher = await _context.Preachers.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            if (preacher != null)
            {
                preacher.Bio = request.Bio;
                preacher.ImageUrl = request.ImageUrl;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
