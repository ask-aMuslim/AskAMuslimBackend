using System.Text.RegularExpressions;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background jobs that deliver support mail notifications.
///
/// <see cref="DispatchAsync"/> works out the recipients for an event, creates in-app notifications and sends pushes
/// (deduplicated per event, so a retry never notifies twice), then queues one <see cref="SendEmailAsync"/> job per email.
/// Email jobs throw on failure so Hangfire retries them.
/// </summary>
public class SupportMailNotificationJobs
{
    public const string NotificationType = "SupportMail";
    public const string PushType = "support_mail";
    public const string DefaultInboxEmail = "info@ask-a-muslim.com";

    private readonly ApplicationDbContext _context;
    private readonly IUserPushService _userPushService;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupportMailNotificationJobs> _logger;

    public SupportMailNotificationJobs(
        ApplicationDbContext context,
        IUserPushService userPushService,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        IConfiguration configuration,
        ILogger<SupportMailNotificationJobs> logger)
    {
        _context = context;
        _userPushService = userPushService;
        _emailService = emailService;
        _backgroundJobClient = backgroundJobClient;
        _configuration = configuration;
        _logger = logger;
    }

    public static string EventKey(SupportMailNotificationEvent notificationEvent) => notificationEvent switch
    {
        SupportMailNotificationEvent.Created => "created",
        SupportMailNotificationEvent.RequesterReplied => "requester_reply",
        SupportMailNotificationEvent.StaffReplied => "staff_reply",
        SupportMailNotificationEvent.Assigned => "assigned",
        SupportMailNotificationEvent.Closed => "closed",
        _ => "unknown"
    };

    /// <param name="relatedId">The new message id for replies, the moderator id for assignments, otherwise null.</param>
    /// <param name="eventId">Unique per queued event; used to deduplicate in-app notifications on retry.</param>
    [AutomaticRetry(Attempts = 3)]
    public async Task DispatchAsync(SupportMailNotificationEvent notificationEvent, Guid mailId, Guid? relatedId, Guid eventId, CancellationToken cancellationToken)
    {
        var mail = await LoadMailAsync(mailId, cancellationToken);
        if (mail == null)
        {
            _logger.LogWarning("Support mail {MailId} not found; skipping {Event} notification", mailId, notificationEvent);
            return;
        }

        var requesterName = FullName(mail.Requester);

        switch (notificationEvent)
        {
            case SupportMailNotificationEvent.Created:
                QueueEmail(notificationEvent, mailId, relatedId, mail.RequesterId);
                QueueEmail(notificationEvent, mailId, relatedId, recipientUserId: null);
                break;

            case SupportMailNotificationEvent.RequesterReplied:
                if (mail.AssignedModeratorId is Guid moderatorId)
                {
                    await NotifyUserAsync(moderatorId, mail, notificationEvent, eventId,
                        $"New reply on Mail #{mail.Number}",
                        $"{requesterName} replied to \"{mail.Subject}\".",
                        cancellationToken);
                    QueueEmail(notificationEvent, mailId, relatedId, moderatorId);
                }
                else
                {
                    QueueEmail(notificationEvent, mailId, relatedId, recipientUserId: null);
                }
                break;

            case SupportMailNotificationEvent.StaffReplied:
                await NotifyUserAsync(mail.RequesterId, mail, notificationEvent, eventId,
                    $"New reply on Mail #{mail.Number}",
                    $"The support team replied to your inquiry \"{mail.Subject}\".",
                    cancellationToken);
                QueueEmail(notificationEvent, mailId, relatedId, mail.RequesterId);
                break;

            case SupportMailNotificationEvent.Assigned:
                if (relatedId is Guid assignedModeratorId)
                {
                    await NotifyUserAsync(assignedModeratorId, mail, notificationEvent, eventId,
                        "New inquiry assigned to you",
                        $"Mail #{mail.Number} from {requesterName}: \"{mail.Subject}\".",
                        cancellationToken);
                    QueueEmail(notificationEvent, mailId, relatedId, assignedModeratorId);
                }
                break;

            case SupportMailNotificationEvent.Closed:
                await NotifyUserAsync(mail.RequesterId, mail, notificationEvent, eventId,
                    $"Mail #{mail.Number} was closed",
                    $"Your inquiry \"{mail.Subject}\" was closed. You can still reply to reopen it.",
                    cancellationToken);
                break;
        }
    }

    /// <param name="recipientUserId">The user to email, or null for the support inbox.</param>
    [AutomaticRetry(Attempts = 5)]
    public async Task SendEmailAsync(SupportMailNotificationEvent notificationEvent, Guid mailId, Guid? relatedId, Guid? recipientUserId, CancellationToken cancellationToken)
    {
        var mail = await LoadMailAsync(mailId, cancellationToken);
        if (mail == null)
        {
            _logger.LogWarning("Support mail {MailId} not found; skipping {Event} email", mailId, notificationEvent);
            return;
        }

        string? to;
        string recipientFirstName = string.Empty;

        if (recipientUserId is Guid userId)
        {
            var recipient = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.FirstName })
                .FirstOrDefaultAsync(cancellationToken);
            to = recipient?.Email;
            recipientFirstName = recipient?.FirstName ?? string.Empty;
        }
        else
        {
            to = _configuration["SupportMails:InboxEmail"];
            if (string.IsNullOrWhiteSpace(to))
            {
                to = DefaultInboxEmail;
            }
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogInformation("No email address for {Event} email on support mail {MailId}; skipping", notificationEvent, mailId);
            return;
        }

        var requesterName = FullName(mail.Requester);
        var topic = Humanize(mail.Topic.ToString());
        var firstMessage = mail.Messages.OrderBy(m => m.Created).Select(m => m.Body).FirstOrDefault() ?? mail.Subject;
        var relatedMessage = relatedId.HasValue
            ? mail.Messages.Where(m => m.Id == relatedId.Value).Select(m => m.Body).FirstOrDefault()
            : null;

        SupportMailEmailTemplates.EmailContent? email = notificationEvent switch
        {
            SupportMailNotificationEvent.Created when recipientUserId.HasValue =>
                SupportMailEmailTemplates.Receipt(recipientFirstName, mail.Number, mail.Subject, topic),
            SupportMailNotificationEvent.Created =>
                SupportMailEmailTemplates.NewMailForInbox(requesterName, mail.Requester?.Email, mail.Number, mail.Subject, topic, firstMessage),
            SupportMailNotificationEvent.RequesterReplied when relatedMessage != null =>
                SupportMailEmailTemplates.RequesterReplyForStaff(requesterName, mail.Requester?.Email, mail.Number, mail.Subject, topic, relatedMessage),
            SupportMailNotificationEvent.StaffReplied when relatedMessage != null =>
                SupportMailEmailTemplates.StaffReplyForRequester(recipientFirstName, mail.Number, mail.Subject, relatedMessage),
            SupportMailNotificationEvent.Assigned =>
                SupportMailEmailTemplates.AssignedToModerator(recipientFirstName, requesterName, mail.Number, mail.Subject, topic, firstMessage),
            _ => null
        };

        if (email == null)
        {
            _logger.LogWarning("No email template for {Event} on support mail {MailId}; skipping", notificationEvent, mailId);
            return;
        }

        await _emailService.SendEmailAsync(to, email.Subject, email.HtmlBody, isHtml: true);
        _logger.LogInformation("Sent {Event} email for support mail {MailId}", notificationEvent, mailId);
    }

    private async Task NotifyUserAsync(
        Guid userId,
        SupportMail mail,
        SupportMailNotificationEvent notificationEvent,
        Guid eventId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var eventKey = EventKey(notificationEvent);
        var actionUrl = $"support-mails/{mail.Id}?event={eventKey}&id={eventId}";

        // A retried job must not create the same in-app notification (or send the same push) twice.
        var alreadySent = await _context.Notifications
            .AnyAsync(n => n.UserId == userId && n.ActionUrl == actionUrl, cancellationToken);
        if (alreadySent)
        {
            return;
        }

        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            NotificationType = NotificationType,
            IsRead = false
        });
        await _context.SaveChangesAsync(cancellationToken);

        await _userPushService.SendToUserAsync(userId, new PushNotificationRequest
        {
            Title = title,
            Body = message,
            CustomData = new Dictionary<string, string>
            {
                ["type"] = PushType,
                ["mailId"] = mail.Id.ToString(),
                ["event"] = eventKey
            }
        }, cancellationToken);
    }

    private void QueueEmail(SupportMailNotificationEvent notificationEvent, Guid mailId, Guid? relatedId, Guid? recipientUserId)
    {
        _backgroundJobClient.Enqueue<SupportMailNotificationJobs>(
            jobs => jobs.SendEmailAsync(notificationEvent, mailId, relatedId, recipientUserId, CancellationToken.None));
    }

    private Task<SupportMail?> LoadMailAsync(Guid mailId, CancellationToken cancellationToken) =>
        _context.SupportMails
            .AsNoTracking()
            .Include(m => m.Requester)
            .Include(m => m.Messages)
            .FirstOrDefaultAsync(m => m.Id == mailId, cancellationToken);

    private static string FullName(User? user) =>
        user == null ? "A user" : $"{user.FirstName} {user.LastName}".Trim();

    private static string Humanize(string enumName) =>
        Regex.Replace(enumName, "(?<!^)([A-Z])", " $1");
}
