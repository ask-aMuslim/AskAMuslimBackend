using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.VerifyOtp;

public record VerifyOtpCommand : IRequest<Result<string>>
{
    public string Email { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
}

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
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
    }
}

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpService _otpService;

    public VerifyOtpCommandHandler(IApplicationDbContext context, IOtpService otpService)
    {
        _context = context;
        _otpService = otpService;
    }

    public async Task<Result<string>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Don't reveal if user exists or not for security
        if (user == null)
        {
            return Result<string>.Failure(new[] { "Invalid or expired OTP." });
        }

        if (!user.IsActive)
        {
            return Result<string>.Failure(new[] { "Account is inactive. Please contact support." });
        }

        // Validate OTP
        var isValidOtp = await _otpService.ValidateOtpAsync(request.Email, request.Otp, cancellationToken);
        if (!isValidOtp)
        {
            return Result<string>.Failure(new[] { "Invalid or expired OTP." });
        }

        // Generate a reset token (JWT or random token) pointing to this email
        string resetToken = await _otpService.GenerateResetTokenAsync(request.Email, cancellationToken);

        return Result<string>.Success(resetToken);
    }
}
