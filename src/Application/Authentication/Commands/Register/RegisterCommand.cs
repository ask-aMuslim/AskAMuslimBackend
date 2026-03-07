using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
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

    public RegisterCommandHandler(IIdentityService identityService, IEmailService emailService)
    {
        _identityService = identityService;
        _emailService = emailService;
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

        if (result.Succeeded && result.Data != null)
        {
            var fullName = $"{request.FirstName} {request.LastName}".Trim();
            var body = $"""
                <html>
                <body style="font-family: Arial, sans-serif; color: #333;">
                  <h2>Welcome to Ask A Muslim, {fullName}!</h2>
                  <p>Thank you for creating your account. We're glad to have you here.</p>
                  <p>You can now log in and start exploring our courses and resources.</p>
                  <br/>
                  <p>If you have any questions, feel free to reach out to us.</p>
                  <p>Warm regards,<br/>The Ask A Muslim Team</p>
                </body>
                </html>
                """;

            await _emailService.SendEmailAsync(request.Email, "Welcome to Ask A Muslim!", body);
        }

        return result;
    }
}
