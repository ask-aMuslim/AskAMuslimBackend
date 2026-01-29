using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace AskAMuslimBackend.Application.Authentication.Commands.ResetPassword;

public record ResetPasswordCommand : IRequest<Result>
{
    public string Email { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email address is required.");

        RuleFor(v => v.Otp)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$")
            .WithMessage("OTP must be a 6-digit number.");

        RuleFor(v => v.NewPassword)
            .NotEmpty()
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long.")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least one number.")
            .Matches(@"[\W_]")
            .WithMessage("Password must contain at least one special character.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpService _otpService;
    private readonly UserManager<Domain.Entities.User> _userManager;

    public ResetPasswordCommandHandler(
        IApplicationDbContext context, 
        IOtpService otpService,
        UserManager<Domain.Entities.User> userManager)
    {
        _context = context;
        _otpService = otpService;
        _userManager = userManager;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate OTP
        var isValidOtp = await _otpService.ValidateOtpAsync(request.Email, request.Otp, cancellationToken);
        if (!isValidOtp)
        {
            return Result.Failure(new[] { "Invalid or expired OTP. Please request a new one." });
        }

        // Find user
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            return Result.Failure(new[] { "User not found." });
        }

        if (!user.IsActive)
        {
            return Result.Failure(new[] { "Account is inactive. Please contact support." });
        }

        // Reset password using UserManager
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return Result.Failure(errors);
        }

        // Remove OTP after successful reset
        await _otpService.RemoveOtpAsync(request.Email, cancellationToken);

        return Result.Success();
    }
}
