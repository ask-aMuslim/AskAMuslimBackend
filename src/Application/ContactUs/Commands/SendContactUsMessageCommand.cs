using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.ContactUs.Commands;

public record SendContactUsMessageCommand : IRequest<Result<bool>>
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class SendContactUsMessageCommandHandler : IRequestHandler<SendContactUsMessageCommand, Result<bool>>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendContactUsMessageCommandHandler> _logger;
    private const string CONTACT_EMAIL = "info@ask-a-muslim.com";

    public SendContactUsMessageCommandHandler(
        IEmailService emailService,
        ILogger<SendContactUsMessageCommandHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(SendContactUsMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subject = $"Contact Us Message from {request.FullName}";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                        <h2 style='color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 10px;'>New Contact Us Message</h2>
                        
                        <div style='margin: 20px 0;'>
                            <p style='margin: 10px 0;'><strong>Full Name:</strong> {request.FullName}</p>
                            <p style='margin: 10px 0;'><strong>Email:</strong> {request.Email}</p>
                        </div>
                        
                        <div style='margin: 20px 0;'>
                            <h3 style='color: #2c3e50; font-size: 16px;'>Message:</h3>
                            <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #3498db; border-radius: 3px;'>
                                {request.Message.Replace("\n", "<br/>")}
                            </div>
                        </div>
                        
                        <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 12px;'>
                            <p>This message was sent via the Contact Us form on {DateTime.UtcNow:f} UTC</p>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(CONTACT_EMAIL, subject, body, isHtml: true);

            _logger.LogInformation(
                "Contact us message sent successfully from {Email} ({FullName})",
                request.Email,
                request.FullName);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send contact us message from {Email} ({FullName})",
                request.Email,
                request.FullName);

            return Result<bool>.Failure(new[] { "Failed to send contact message. Please try again later." });
        }
    }
}

public class SendContactUsMessageCommandValidator : AbstractValidator<SendContactUsMessageCommand>
{
    public SendContactUsMessageCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(100)
            .WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.")
            .MaximumLength(255)
            .WithMessage("Email must not exceed 255 characters.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required.")
            .MinimumLength(10)
            .WithMessage("Message must be at least 10 characters long.")
            .MaximumLength(2000)
            .WithMessage("Message must not exceed 2000 characters.");
    }
}
