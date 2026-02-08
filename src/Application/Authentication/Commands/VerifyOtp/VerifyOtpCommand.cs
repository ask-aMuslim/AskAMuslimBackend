using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.VerifyOtp;

public record VerifyOtpCommand : IRequest<Result>
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

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpService _otpService;

    public VerifyOtpCommandHandler(IApplicationDbContext context, IOtpService otpService)
    {
        _context = context;
        _otpService = otpService;
    }

    public async Task<Result> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Don't reveal if user exists or not for security
        if (user == null)
        {
            return Result.Failure(new[] { "Invalid or expired OTP." });
        }

        if (!user.IsActive)
        {
            return Result.Failure(new[] { "Account is inactive. Please contact support." });
        }

        // Validate OTP
        var isValidOtp = await _otpService.ValidateOtpAsync(request.Email, request.Otp, cancellationToken);
        if (!isValidOtp)
        {
            return Result.Failure(new[] { "Invalid or expired OTP." });
        }

        return Result.Success();
    }
}
