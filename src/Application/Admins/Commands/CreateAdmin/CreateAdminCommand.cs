using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace AskAMuslimBackend.Application.Admins.Commands.CreateAdmin;

[Authorize(Policy = Policies.ManageAdmins)]
public record CreateAdminCommand : IRequest<Result<Guid>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
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

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => role == Roles.Administrator || role == Roles.Instructor || role == Roles.Preacher)
            .WithMessage("Role must be Administrator, Instructor, or Preacher");
    }
}

public class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, Result<Guid>>
{
    private readonly UserManager<Domain.Entities.User> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly IPermissionService _permissionService;

    public CreateAdminCommandHandler(
        UserManager<Domain.Entities.User> userManager,
        IApplicationDbContext context,
        IPermissionService permissionService)
    {
        _userManager = userManager;
        _context = context;
        _permissionService = permissionService;
    }

    public async Task<Result<Guid>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Result<Guid>.Failure(new[] { "User with this email already exists" });
        }

        // Create user
        var user = new Domain.Entities.User
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            EmailConfirmed = true // Auto-confirm admin emails
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result<Guid>.Failure(createResult.Errors.Select(e => e.Description).ToArray());
        }

        // Assign role
        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            // Rollback user creation if role assignment fails
            await _userManager.DeleteAsync(user);
            return Result<Guid>.Failure(roleResult.Errors.Select(e => e.Description).ToArray());
        }

        // Create corresponding entity based on role
        if (request.Role == Roles.Administrator)
        {
            var admin = new Domain.Entities.Admin
            {
                UserId = user.Id,
                Bio = request.Bio,
                ImageUrl = request.ImageUrl
            };
            _context.Admins.Add(admin);
        }
        else if (request.Role == Roles.Instructor)
        {
            var instructor = new Domain.Entities.Instructor
            {
                UserId = user.Id,
                Bio = request.Bio,
                ImageUrl = request.ImageUrl
            };
            _context.Instructors.Add(instructor);
        }
        else if (request.Role == Roles.Preacher)
        {
            var preacher = new Domain.Entities.Preacher
            {
                UserId = user.Id,
                Bio = request.Bio,
                ImageUrl = request.ImageUrl
            };
            _context.Preachers.Add(preacher);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Grant the user default permissions for their role
        var rolePermissions = await _permissionService.GetDefaultPermissionsForRoleAsync(request.Role);
        await _permissionService.SetUserPermissionsAsync(user.Id.ToString(), rolePermissions, null, $"Auto-granted on user creation for role {request.Role}", cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}
