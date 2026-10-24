using System.Text.RegularExpressions;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.MeetingRequests.Common;
using AskAMuslimBackend.Application.Notifications.Common.Interfaces;
using AskAMuslimBackend.Application.Notifications.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background jobs that deliver meeting request notifications.
///
/// <see cref="DispatchAsync"/> works out the recipients for an event, creates in-app notifications and sends pushes
/// (deduplicated per event, so a retry never notifies twice), then queues one <see cref="SendEmailAsync"/> job per email.
/// Email jobs throw on failure so Hangfire retries them.
/// </summary>
public class MeetingRequestNotificationJobs
{
    public const string NotificationType = "MeetingRequest";
    public const string PushType = "meeting_request";
    public const string DefaultWebBaseUrl = "https://askamuslim.com";

    private readonly ApplicationDbContext _context;
    private readonly IUserPushService _userPushService;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MeetingRequestNotificationJobs> _logger;

    public MeetingRequestNotificationJobs(
        ApplicationDbContext context,
        IUserPushService userPushService,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        IConfiguration configuration,
        ILogger<MeetingRequestNotificationJobs> logger)
    {
        _context = context;
        _userPushService = userPushService;
        _emailService = emailService;
        _backgroundJobClient = backgroundJobClient;
        _configuration = configuration;
        _logger = logger;
    }

    public static string EventKey(MeetingRequestNotificationEvent notificationEvent) => notificationEvent switch
    {
        MeetingRequestNotificationEvent.Created => "created",
        MeetingRequestNotificationEvent.Assigned => "assigned",
        MeetingRequestNotificationEvent.Accepted => "accepted",
        MeetingRequestNotificationEvent.Rejected => "rejected",
        MeetingRequestNotificationEvent.Cancelled => "cancelled",
        MeetingRequestNotificationEvent.Completed => "completed",
        MeetingRequestNotificationEvent.ResponseAdded => "response",
        _ => "unknown"
    };

    /// <param name="relatedId">The moderator id for assignments, the acting user id for status changes, otherwise null.</param>
    /// <param name="eventId">Unique per queued event; used to deduplicate in-app notifications on retry.</param>
    [AutomaticRetry(Attempts = 3)]
    public async Task DispatchAsync(MeetingRequestNotificationEvent notificationEvent, Guid requestId, Guid? relatedId, Guid eventId, CancellationToken cancellationToken)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        if (request == null)
        {
            _logger.LogWarning("Meeting request {RequestId} not found; skipping {Event} notification", requestId, notificationEvent);
            return;
        }

        var requesterName = FullName(request.Requester);
        var topic = Humanize(request.Topic.ToString());
        var when = FormatWhen(request.ScheduledAt);

        switch (notificationEvent)
        {
            case MeetingRequestNotificationEvent.Created:
                QueueEmail(notificationEvent, requestId, relatedId, request.RequesterId);
                QueueEmail(notificationEvent, requestId, relatedId, recipientUserId: null);
                break;

            case MeetingRequestNotificationEvent.Assigned:
                if (relatedId is Guid assignedModeratorId)
                {
                    await NotifyUserAsync(assignedModeratorId, request, notificationEvent, eventId,
                        "New meeting request assigned to you",
                        $"{requesterName} requested a meeting about {topic} on {when}.",
                        cancellationToken);
                    QueueEmail(notificationEvent, requestId, relatedId, assignedModeratorId);
                }
                break;

            case MeetingRequestNotificationEvent.Accepted:
                await NotifyUserAsync(request.RequesterId, request, notificationEvent, eventId,
                    "Your meeting request was accepted",
                    $"Your meeting about {topic} on {when} was accepted.",
                    cancellationToken);
                QueueEmail(notificationEvent, requestId, relatedId, request.RequesterId);
                break;

            case MeetingRequestNotificationEvent.Rejected:
                await NotifyUserAsync(request.RequesterId, request, notificationEvent, eventId,
                    "Your meeting request was declined",
                    $"Your meeting request about {topic} on {when} could not be accepted.",
                    cancellationToken);
                QueueEmail(notificationEvent, requestId, relatedId, request.RequesterId);
                break;

            case MeetingRequestNotificationEvent.Completed:
                await NotifyUserAsync(request.RequesterId, request, notificationEvent, eventId,
                    "Your meeting was completed",
                    $"Your meeting about {topic} on {when} was marked as completed.",
                    cancellationToken);
                QueueEmail(notificationEvent, requestId, relatedId, request.RequesterId);
                break;

            case MeetingRequestNotificationEvent.Cancelled:
                // Tell the other side: the moderator when the requester cancelled, otherwise the requester.
                var cancelledByRequester = relatedId == request.RequesterId;
                var recipientId = cancelledByRequester ? request.ModeratorId : request.RequesterId;
                if (recipientId is Guid cancelRecipient)
                {
                    await NotifyUserAsync(cancelRecipient, request, notificationEvent, eventId,
                        "Meeting request cancelled",
                        cancelledByRequester
                            ? $"{requesterName} cancelled the meeting about {topic} on {when}."
                            : $"Your meeting request about {topic} on {when} was cancelled.",
                        cancellationToken);
                }
                break;

            case MeetingRequestNotificationEvent.ResponseAdded:
                await NotifyUserAsync(request.RequesterId, request, notificationEvent, eventId,
                    "New response to your meeting request",
                    $"The team responded to your meeting request about {topic}.",
                    cancellationToken);
                QueueEmail(notificationEvent, requestId, relatedId, request.RequesterId);
                break;
        }
    }

    /// <param name="recipientUserId">The user to email, or null for the support inbox.</param>
    [AutomaticRetry(Attempts = 5)]
    public async Task SendEmailAsync(MeetingRequestNotificationEvent notificationEvent, Guid requestId, Guid? relatedId, Guid? recipientUserId, CancellationToken cancellationToken)
    {
        var request = await LoadAsync(requestId, cancellationToken);
        if (request == null)
        {
            _logger.LogWarning("Meeting request {RequestId} not found; skipping {Event} email", requestId, notificationEvent);
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
            to = _configuration["MeetingRequests:InboxEmail"];
            if (string.IsNullOrWhiteSpace(to))
            {
                to = _configuration["SupportMails:InboxEmail"];
            }
            if (string.IsNullOrWhiteSpace(to))
            {
                to = SupportMailNotificationJobs.DefaultInboxEmail;
            }
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogInformation("No email address for {Event} email on meeting request {RequestId}; skipping", notificationEvent, requestId);
            return;
        }

        var requesterName = FullName(request.Requester);
        var topic = Humanize(request.Topic.ToString());
        var when = FormatWhen(request.ScheduledAt);
        var viewUrl = ViewUrl(request.Id);

        SupportMailEmailTemplates.EmailContent? email = notificationEvent switch
        {
            MeetingRequestNotificationEvent.Created when recipientUserId.HasValue =>
                MeetingRequestEmailTemplates.Receipt(recipientFirstName, topic, when, viewUrl),
            MeetingRequestNotificationEvent.Created =>
                MeetingRequestEmailTemplates.NewRequestForInbox(requesterName, request.Requester?.Email, topic, when, request.Message),
            MeetingRequestNotificationEvent.Assigned =>
                MeetingRequestEmailTemplates.AssignedToModerator(recipientFirstName, requesterName, topic, when, request.Message),
            MeetingRequestNotificationEvent.Accepted =>
                MeetingRequestEmailTemplates.Accepted(recipientFirstName, topic, when, ModeratorName(request, relatedId), viewUrl),
            MeetingRequestNotificationEvent.Rejected =>
                MeetingRequestEmailTemplates.Rejected(recipientFirstName, topic, when, viewUrl),
            MeetingRequestNotificationEvent.Completed =>
                MeetingRequestEmailTemplates.Completed(recipientFirstName, topic, when, viewUrl),
            MeetingRequestNotificationEvent.ResponseAdded when !string.IsNullOrWhiteSpace(request.Response) =>
                MeetingRequestEmailTemplates.ResponseForRequester(recipientFirstName, topic, when, request.Response, viewUrl),
            _ => null
        };

        if (email == null)
        {
            _logger.LogWarning("No email template for {Event} on meeting request {RequestId}; skipping", notificationEvent, requestId);
            return;
        }

        await _emailService.SendEmailAsync(to, email.Subject, email.HtmlBody, isHtml: true);
        _logger.LogInformation("Sent {Event} email for meeting request {RequestId}", notificationEvent, requestId);
    }

    private async Task NotifyUserAsync(
        Guid userId,
        MeetingRequest request,
        MeetingRequestNotificationEvent notificationEvent,
        Guid eventId,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        var eventKey = EventKey(notificationEvent);
        var actionUrl = $"meeting-requests/{request.Id}?event={eventKey}&id={eventId}";

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
                ["meetingRequestId"] = request.Id.ToString(),
                ["event"] = eventKey
            }
        }, cancellationToken);
    }

    private void QueueEmail(MeetingRequestNotificationEvent notificationEvent, Guid requestId, Guid? relatedId, Guid? recipientUserId)
    {
        _backgroundJobClient.Enqueue<MeetingRequestNotificationJobs>(
            jobs => jobs.SendEmailAsync(notificationEvent, requestId, relatedId, recipientUserId, CancellationToken.None));
    }

    private Task<MeetingRequest?> LoadAsync(Guid requestId, CancellationToken cancellationToken) =>
        _context.MeetingRequests
            .AsNoTracking()
            .Include(m => m.Requester)
            .Include(m => m.Moderator)
            .FirstOrDefaultAsync(m => m.Id == requestId, cancellationToken);

    /// <summary>The request's page on the website. Set <c>MeetingRequests:WebBaseUrl</c> to point emails at another site.</summary>
    private string ViewUrl(Guid requestId)
    {
        var baseUrl = _configuration["MeetingRequests:WebBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultWebBaseUrl;
        }

        return $"{baseUrl.TrimEnd('/')}/question-and-answer/meet-scholar/history/{requestId}";
    }

    private static string? ModeratorName(MeetingRequest request, Guid? actorId)
    {
        var moderator = request.Moderator;
        return moderator != null && (actorId == null || moderator.Id == actorId) ? FullName(moderator) : null;
    }

    private static string FullName(User? user) =>
        user == null ? "A user" : $"{user.FirstName} {user.LastName}".Trim();

    private static string FormatWhen(DateTime scheduledAt) =>
        $"{scheduledAt:yyyy-MM-dd HH:mm} UTC";

    private static string Humanize(string enumName) =>
        Regex.Replace(enumName, "(?<!^)([A-Z])", " $1");
}
