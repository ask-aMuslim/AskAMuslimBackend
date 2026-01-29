using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.ForgotPassword;

public record ForgotPasswordCommand : IRequest<Result>
{
    public string Email { get; init; } = string.Empty;
}

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email address is required.");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpService _otpService;

    public ForgotPasswordCommandHandler(IApplicationDbContext context, IOtpService otpService)
    {
        _context = context;
        _otpService = otpService;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        // Always return success to prevent email enumeration attacks
        if (user == null)
        {
            return Result.Success();
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result.Success(); // Don't reveal that account is inactive
        }

        // Generate and send OTP
        var otp = await _otpService.GenerateOtpAsync(request.Email, cancellationToken);
        await _otpService.SendOtpEmailAsync(request.Email, otp, user.FirstName, cancellationToken);

        return Result.Success();
    }
}
