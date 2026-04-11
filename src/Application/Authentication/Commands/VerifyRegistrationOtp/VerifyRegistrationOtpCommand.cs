using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.Authentication.Commands.VerifyRegistrationOtp;

public record VerifyRegistrationOtpCommand : IRequest<Result<AuthenticationResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Otp { get; init; } = string.Empty;
}

public class VerifyRegistrationOtpCommandValidator : AbstractValidator<VerifyRegistrationOtpCommand>
{
    public VerifyRegistrationOtpCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty()
            .EmailAddress();
            
        RuleFor(v => v.Otp)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(6);
    }
}

public class VerifyRegistrationOtpCommandHandler : IRequestHandler<VerifyRegistrationOtpCommand, Result<AuthenticationResponse>>
{
    private readonly IOtpService _otpService;
    private readonly IIdentityService _identityService;

    public VerifyRegistrationOtpCommandHandler(IOtpService otpService, IIdentityService identityService)
    {
        _otpService = otpService;
        _identityService = identityService;
    }

    public async Task<Result<AuthenticationResponse>> Handle(VerifyRegistrationOtpCommand request, CancellationToken cancellationToken)
    {
        var isValid = await _otpService.ValidateOtpAsync(request.Email, request.Otp, cancellationToken);
        
        if (!isValid)
        {
            return Result<AuthenticationResponse>.Failure(new[] { "Invalid or expired OTP." });
        }

        var result = await _identityService.ConfirmEmailAsync(request.Email);
        return result;
    }
}