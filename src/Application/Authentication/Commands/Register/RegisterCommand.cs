using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Hangfire;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Authentication.Commands.Register;

public record RegisterCommand : IRequest<Result<AuthenticationResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public UserRole Role { get; init; } = UserRole.Student;
    public ReligiousStatus Religion { get; init; }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthenticationResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public RegisterCommandHandler(
        IIdentityService identityService, 
        IEmailService emailService, 
        IOtpService otpService,
        IBackgroundJobClient backgroundJobClient)
    {
        _identityService = identityService;
        _emailService = emailService;
        _otpService = otpService;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<Result<AuthenticationResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.RegisterUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Role,
            request.Religion,
            null,
            cancellationToken);

        if (result.Succeeded && result.Data != null && !string.IsNullOrEmpty(result.Data.UserId))
        {
            var otp = await _otpService.GenerateOtpAsync(request.Email, cancellationToken);
            
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var body = $"""
                <html>
                <body style="font-family: Arial, sans-serif; color: #333;">
                  <h2>Welcome to Ask A Muslim, {fullName}!</h2>
                  <p>Thank you for creating your account. To verify your email address, please use the following OTP:</p>
                  <h3 style="font-size: 24px; color: #d32f2f;">{otp}</h3>
                  <p>This OTP is valid for 10 minutes.</p>
                  <br/>
                  <p>If you have any questions, feel free to reach out to us.</p>
                  <p>Warm regards,<br/>The Ask A Muslim Team</p>
                </body>
                </html>
                """;

            // Dispatch the email securely over a Background Thread to avoid blocking users
            _backgroundJobClient.Enqueue(() => 
                _emailService.SendEmailAsync(request.Email, "Verify Your Ask A Muslim Account", body, true));
        }

        return result;
    }
}
