using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Newsletters.Commands.SendNewsletter;

public record SendNewsletterCommand : IRequest<Result>
{
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
}

public class SendNewsletterCommandValidator : AbstractValidator<SendNewsletterCommand>
{
    public SendNewsletterCommandValidator()
    {
        RuleFor(v => v.Subject)
            .NotEmpty()
            .WithMessage("Subject is required.");

        RuleFor(v => v.HtmlBody)
            .NotEmpty()
            .WithMessage("HTML body is required.");
    }
}

public class SendNewsletterCommandHandler : IRequestHandler<SendNewsletterCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public SendNewsletterCommandHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Result> Handle(SendNewsletterCommand request, CancellationToken cancellationToken)
    {
        var activeSubscribers = await _context.NewsletterSubscriptions
            .Where(n => n.IsActive)
            .Select(n => n.Email)
            .ToListAsync(cancellationToken);

        if (!activeSubscribers.Any())
        {
            return Result.Failure(new[] { "No active subscribers found." });
        }

        // Basic wrapper template for the email to make it look good
        var fullHtmlBody = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{request.Subject}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333333;
            margin: 0;
            padding: 0;
            background-color: #f9f9f9;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background-color: #2c3e50;
            color: #ffffff;
            padding: 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .content {{
            padding: 30px;
        }}
        .footer {{
            background-color: #ecf0f1;
            color: #7f8c8d;
            text-align: center;
            padding: 15px;
            font-size: 14px;
            border-top: 1px solid #e0e0e0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Ask A Muslim</h1>
        </div>
        <div class=""content"">
            {request.HtmlBody}
        </div>
        <div class=""footer"">
            <p>You received this email because you are subscribed to our newsletter.</p>
            <p>&copy; {DateTime.UtcNow.Year} Ask A Muslim. All rights reserved.</p>
            <!-- Add unsubscription link logic here in the future if needed -->
        </div>
    </div>
</body>
</html>";

        // Dispatch emails in bulk assuming SendBulkEmailAsync handles batching properly
        await _emailService.SendBulkEmailAsync(
            recipients: activeSubscribers,
            subject: request.Subject,
            body: fullHtmlBody,
            isHtml: true
        );

        return Result.Success();
    }
}