using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services;

public class EventNotificationService : IEventNotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventNotificationService> _logger;

    public EventNotificationService(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<EventNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendEventNotificationAsync(
        Guid eventId,
        string eventTitle,
        string? description,
        DateTime startDateTime,
        DateTime endDateTime,
        string? meetingLink,
        string? speakerName,
        UserRole? targetRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get target users based on role
            var usersQuery = _context.Users.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email));

            if (targetRole.HasValue)
            {
                // Specific role
                usersQuery = usersQuery.Where(u => u.Role == targetRole.Value);
                _logger.LogInformation("Sending event notification to users with role: {Role}", targetRole.Value);
            }
            else
            {
                // General event - all users
                _logger.LogInformation("Sending general event notification to all users");
            }

            var users = await usersQuery
                .Select(u => new { u.Email, u.FirstName, u.LastName })
                .ToListAsync(cancellationToken);

            if (!users.Any())
            {
                _logger.LogWarning("No users found to notify for event: {EventTitle}", eventTitle);
                return;
            }

            _logger.LogInformation("Sending event notification to {Count} users", users.Count);

            // Send emails in batches to avoid overwhelming the email service
            var batchSize = 50;
            for (int i = 0; i < users.Count; i += batchSize)
            {
                var batch = users.Skip(i).Take(batchSize);
                var emailTasks = batch.Select(user =>
                    SendEventEmailAsync(
                        user.Email!,
                        user.FirstName,
                        eventTitle,
                        description,
                        startDateTime,
                        endDateTime,
                        meetingLink,
                        speakerName,
                        targetRole));

                await Task.WhenAll(emailTasks);
            }

            _logger.LogInformation("Event notification sent successfully for: {EventTitle}", eventTitle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event notification for: {EventTitle}", eventTitle);
            // Don't throw - we don't want to fail event creation if email fails
        }
    }

    private async Task SendEventEmailAsync(
        string email,
        string userName,
        string eventTitle,
        string? description,
        DateTime startDateTime,
        DateTime endDateTime,
        string? meetingLink,
        string? speakerName,
        UserRole? targetRole)
    {
        var subject = $"New Event: {eventTitle}";
        var body = GenerateEventEmailHtml(
            userName,
            eventTitle,
            description,
            startDateTime,
            endDateTime,
            meetingLink,
            speakerName,
            targetRole);

        await _emailService.SendEmailAsync(email, subject, body, isHtml: true);
    }

    private static string GenerateEventEmailHtml(
        string userName,
        string eventTitle,
        string? description,
        DateTime startDateTime,
        DateTime endDateTime,
        string? meetingLink,
        string? speakerName,
        UserRole? targetRole)
    {
        var roleSection = targetRole.HasValue
            ? $@"<div class=""role-badge"">
                    <span class=""badge"">👥 For {GetRoleDisplayName(targetRole.Value)}</span>
                 </div>"
            : @"<div class=""role-badge"">
                    <span class=""badge general"">🌍 General Event - Open to All</span>
                 </div>";

        var speakerSection = !string.IsNullOrEmpty(speakerName)
            ? $@"<div class=""info-item"">
                    <span class=""icon"">🎤</span>
                    <div>
                        <div class=""label"">Speaker</div>
                        <div class=""value"">{speakerName}</div>
                    </div>
                 </div>"
            : "";

        var meetingLinkSection = !string.IsNullOrEmpty(meetingLink)
            ? $@"<div class=""meeting-link"">
                    <a href=""{meetingLink}"" class=""btn"">📹 Join Meeting</a>
                 </div>"
            : "";

        var descriptionSection = !string.IsNullOrEmpty(description)
            ? $@"<div class=""description"">
                    <h3>About This Event</h3>
                    <p>{description}</p>
                 </div>"
            : "";

        var duration = endDateTime - startDateTime;
        var durationText = duration.TotalHours >= 1
            ? $"{duration.TotalHours:F1} hours"
            : $"{duration.TotalMinutes:F0} minutes";

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Event Notification</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f7fa;
            margin: 0;
            padding: 0;
        }}
        .email-container {{
            max-width: 650px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #4CAF50 0%, #2E7D32 100%);
            padding: 40px 30px;
            text-align: center;
            color: #ffffff;
        }}
        .header h1 {{
            margin: 0 0 10px 0;
            font-size: 32px;
            font-weight: 700;
        }}
        .header-subtitle {{
            font-size: 16px;
            opacity: 0.9;
            margin: 0;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .greeting {{
            font-size: 18px;
            margin-bottom: 20px;
            color: #333333;
        }}
        .event-title {{
            font-size: 28px;
            font-weight: 700;
            color: #2E7D32;
            margin: 20px 0;
            text-align: center;
        }}
        .role-badge {{
            text-align: center;
            margin: 20px 0;
        }}
        .badge {{
            display: inline-block;
            background: linear-gradient(135deg, #4CAF50, #66BB6A);
            color: white;
            padding: 10px 25px;
            border-radius: 25px;
            font-size: 14px;
            font-weight: 600;
        }}
        .badge.general {{
            background: linear-gradient(135deg, #2196F3, #42A5F5);
        }}
        .event-details {{
            background-color: #f8f9fa;
            border-radius: 8px;
            padding: 25px;
            margin: 30px 0;
        }}
        .info-item {{
            display: flex;
            align-items: flex-start;
            margin-bottom: 20px;
        }}
        .info-item:last-child {{
            margin-bottom: 0;
        }}
        .icon {{
            font-size: 24px;
            margin-right: 15px;
            min-width: 30px;
        }}
        .label {{
            font-size: 12px;
            color: #666666;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 5px;
        }}
        .value {{
            font-size: 16px;
            color: #333333;
            font-weight: 600;
        }}
        .description {{
            margin: 25px 0;
            padding: 20px;
            background-color: #f8f9fa;
            border-left: 4px solid #4CAF50;
            border-radius: 4px;
        }}
        .description h3 {{
            margin: 0 0 15px 0;
            font-size: 18px;
            color: #2E7D32;
        }}
        .description p {{
            margin: 0;
            line-height: 1.6;
            color: #555555;
        }}
        .meeting-link {{
            text-align: center;
            margin: 30px 0;
        }}
        .btn {{
            display: inline-block;
            background: linear-gradient(135deg, #4CAF50, #2E7D32);
            color: white;
            padding: 15px 40px;
            text-decoration: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3);
            transition: transform 0.2s;
        }}
        .btn:hover {{
            transform: translateY(-2px);
        }}
        .reminder {{
            background-color: #e3f2fd;
            border-left: 4px solid #2196F3;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
        .reminder-text {{
            font-size: 14px;
            color: #0d47a1;
            margin: 0;
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
        .divider {{
            height: 1px;
            background: linear-gradient(to right, transparent, #e0e0e0, transparent);
            margin: 25px 0;
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>📅 Event Invitation</h1>
            <p class=""header-subtitle"">You're invited to join us!</p>
        </div>
        
        <div class=""content"">
            <div class=""greeting"">
                Hello {userName},
            </div>
            
            <p style=""font-size: 16px; color: #555555; line-height: 1.6;"">
                We're excited to invite you to an upcoming event on Ask A Muslim platform.
            </p>
            
            <div class=""event-title"">{eventTitle}</div>
            
            {roleSection}
            
            <div class=""event-details"">
                <div class=""info-item"">
                    <span class=""icon"">📅</span>
                    <div>
                        <div class=""label"">Date & Time</div>
                        <div class=""value"">{startDateTime.ToString("dddd, MMMM dd, yyyy")}</div>
                        <div class=""value"">{startDateTime.ToString("hh:mm tt")} - {endDateTime.ToString("hh:mm tt")} UTC</div>
                    </div>
                </div>
                
                <div class=""divider""></div>
                
                <div class=""info-item"">
                    <span class=""icon"">⏱️</span>
                    <div>
                        <div class=""label"">Duration</div>
                        <div class=""value"">{durationText}</div>
                    </div>
                </div>
                
                {(speakerSection != "" ? @"<div class=""divider""></div>" + speakerSection : "")}
            </div>
            
            {descriptionSection}
            
            {meetingLinkSection}
            
            <div class=""reminder"">
                <p class=""reminder-text"">
                    <strong>💡 Reminder:</strong> Add this event to your calendar and set a reminder 
                    so you don't miss it!
                </p>
            </div>
        </div>
        
        <div class=""footer"">
            <p class=""footer-text"">
                Have questions? <a href=""mailto:support@ask-a-muslim.com"" style=""color: #4CAF50; text-decoration: none;"">Contact Support</a>
            </p>
            <p class=""footer-text"">
                © 2026 Ask A Muslim. All rights reserved.
            </p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GetRoleDisplayName(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "Administrators",
            UserRole.Student => "Students",
            UserRole.Instructor => "Instructors",
            UserRole.Preacher => "Preachers",
            UserRole.NonMuslim => "Non-Muslims",
            _ => "All Users"
        };
    }
}
