using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace AskAMuslimBackend.Application.Admins.Commands.CreateAdmin;

[Authorize(Policy = Policies.ManageAdmins)]
public record CreateAdminCommand : IRequest<Result<ModeratorCreatedResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
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

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => role == Roles.Administrator || role == Roles.Instructor || role == Roles.Preacher)
            .WithMessage("Role must be Administrator, Instructor, or Preacher");
    }
}

public class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, Result<ModeratorCreatedResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public CreateAdminCommandHandler(
        IIdentityService identityService,
        IApplicationDbContext context,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _identityService = identityService;
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<Result<ModeratorCreatedResponse>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        // Auto-generate password if not provided
        var password = GenerateRandomPassword();

        // Map role string to UserRole enum
        var userRole = (UserRole)Enum.Parse(typeof(UserRole), request.Role, true);

        // Use IdentityService to register the user
        // allowAdminCreation: true - This command is protected by Policies.ManageAdmins
        var result = await _identityService.RegisterUserAsync(
            request.Email,
            password,
            request.FirstName,
            request.LastName,
            userRole,
            ReligiousStatus.BornMuslim, // Default to BornMuslim for admins
            null, // No image file in this command
            cancellationToken,
            allowAdminCreation: true); // ← Allow admin creation (authorized by policy)

        if (!result.Succeeded)
        {
            return Result<ModeratorCreatedResponse>.Failure(result.Errors);
        }

        // Parse the userId from the response
        if (result.Data == null || !Guid.TryParse(result.Data.UserId, out Guid userId))
        {
            return Result<ModeratorCreatedResponse>.Failure(new[] { "Failed to parse user ID" });
        }

        // Update additional fields if provided (Bio, ImageUrl, PhoneNumber)
        if (!string.IsNullOrWhiteSpace(request.Bio) || !string.IsNullOrWhiteSpace(request.ImageUrl) || !string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            await UpdateAdditionalFields(userId, request, userRole, cancellationToken);
        }

        // Send welcome email with admin panel link and password
        await SendWelcomeEmailAsync(request.Email, request.FirstName, request.Role, password, cancellationToken);

        var response = new ModeratorCreatedResponse
        {
            UserId = userId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            GeneratedPassword = password
        };

        return Result<ModeratorCreatedResponse>.Success(response);
    }

    private async Task UpdateAdditionalFields(Guid userId, CreateAdminCommand request, UserRole userRole, CancellationToken cancellationToken)
    {
        // Update user phone number if provided
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user != null)
            {
                user.PhoneNumber = request.PhoneNumber;
            }
        }

        // Update role-specific entity with Bio and ImageUrl
        if (userRole == UserRole.Admin)
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
            if (admin != null)
            {
                admin.Bio = request.Bio;
                admin.ImageUrl = request.ImageUrl;
            }
        }
        else if (userRole == UserRole.Instructor)
        {
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userId, cancellationToken);
            if (instructor != null)
            {
                instructor.Bio = request.Bio;
                instructor.ImageUrl = request.ImageUrl;
            }
        }
        else if (userRole == UserRole.Preacher)
        {
            var preacher = await _context.Preachers.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
            if (preacher != null)
            {
                preacher.Bio = request.Bio;
                preacher.ImageUrl = request.ImageUrl;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SendWelcomeEmailAsync(string email, string firstName, string role, string password, CancellationToken cancellationToken)
    {
        try
        {
            var adminPanelUrl = _configuration["AdminPanel:Url"] ?? "https://admin.ask-a-muslim.com";
            var subject = $"Welcome to Ask A Muslim - {role} Access";
            var body = GenerateWelcomeEmailHtml(firstName, role, password, adminPanelUrl);

            await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
        }
        catch (Exception)
        {
            // Don't fail the command if email sending fails
            // The moderator was created successfully
        }
    }

    private static string GenerateWelcomeEmailHtml(string firstName, string role, string password, string adminPanelUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to Ask A Muslim</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7fa;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #FF9800 0%, #F57C00 100%);
            padding: 40px 30px;
            text-align: center;
            color: #ffffff;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .greeting {{
            font-size: 18px;
            margin-bottom: 20px;
            color: #333333;
        }}
        .message {{
            font-size: 16px;
            line-height: 1.8;
            color: #555555;
            margin: 20px 0;
        }}
        .role-badge {{
            display: inline-block;
            background: linear-gradient(135deg, #FF9800, #F57C00);
            color: white;
            padding: 10px 20px;
            border-radius: 20px;
            font-weight: 600;
            margin: 10px 0;
        }}
        .action-section {{
            text-align: center;
            margin: 30px 0;
        }}
        .btn {{
            display: inline-block;
            background: linear-gradient(135deg, #FF9800, #F57C00);
            color: white;
            padding: 15px 40px;
            text-decoration: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            box-shadow: 0 4px 12px rgba(255, 152, 0, 0.3);
        }}
        .info-box {{
            background-color: #f8f9fa;
            padding: 20px;
            border-left: 4px solid #FF9800;
            border-radius: 4px;
            margin: 20px 0;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 30px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer-text {{
            font-size: 13px;
            color: #6c757d;
            margin: 5px 0;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>🎉 Welcome to Ask A Muslim!</h1>
        </div>

        <div class=""content"">
            <div class=""greeting"">
                Hello {firstName},
            </div>

            <div class=""message"">
                Congratulations! You have been granted <span class=""role-badge"">{role}</span> access to the Ask A Muslim platform.
            </div>

            <div class=""message"">
                Your account has been successfully created. You can now access the admin panel to manage the platform and contribute to our community.
            </div>

            <div class=""info-box"">
                <strong>📧 Your Login Email:</strong> Your registered email address will be used to log in.
                <br><br>
                <strong>🔐 Your Password:</strong> <code style=""background-color: #fff3cd; padding: 5px 10px; border-radius: 4px; font-size: 14px; color: #856404; font-weight: 600;"">{password}</code>
                <br><br>
                <strong>⚠️ Important:</strong> Please keep this password confidential and change it after your first login for security.
            </div>

            <div class=""action-section"">
                <a href=""{adminPanelUrl}"" class=""btn"">Access Admin Panel</a>
            </div>

            <div class=""message"">
                If you have any questions or need assistance, please don't hesitate to contact the system administrator.
            </div>
        </div>

        <div class=""footer"">
            <p class=""footer-text"">
                © 2026 Ask A Muslim. All rights reserved.
            </p>
            <p class=""footer-text"">
                This is an automated message. Please do not reply to this email.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GenerateRandomPassword()
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var random = new Random();
        var chars = new char[16];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = validChars[random.Next(validChars.Length)];
        }
        return new string(chars);
    }
}
