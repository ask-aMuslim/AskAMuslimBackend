using AskAMuslimBackend.Application.SupportMails.Common;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Hangfire-backed <see cref="ISupportMailNotifier"/>. Jobs carry ids only and reload data when they run.
/// </summary>
public class SupportMailNotifier : ISupportMailNotifier
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SupportMailNotifier> _logger;

    public SupportMailNotifier(IBackgroundJobClient backgroundJobClient, ILogger<SupportMailNotifier> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public void MailCreated(Guid mailId) =>
        Enqueue(SupportMailNotificationEvent.Created, mailId, null);

    public void RequesterReplied(Guid mailId, Guid messageId) =>
        Enqueue(SupportMailNotificationEvent.RequesterReplied, mailId, messageId);

    public void StaffReplied(Guid mailId, Guid messageId) =>
        Enqueue(SupportMailNotificationEvent.StaffReplied, mailId, messageId);

    public void Assigned(Guid mailId, Guid moderatorId) =>
        Enqueue(SupportMailNotificationEvent.Assigned, mailId, moderatorId);

    public void Closed(Guid mailId) =>
        Enqueue(SupportMailNotificationEvent.Closed, mailId, null);

    private void Enqueue(SupportMailNotificationEvent notificationEvent, Guid mailId, Guid? relatedId)
    {
        try
        {
            var eventId = Guid.NewGuid();
            _backgroundJobClient.Enqueue<SupportMailNotificationJobs>(
                jobs => jobs.DispatchAsync(notificationEvent, mailId, relatedId, eventId, CancellationToken.None));
        }
        catch (Exception ex)
        {
            // The mail change is already saved; a queueing failure must not fail the request.
            _logger.LogError(ex, "Failed to queue {Event} notification for support mail {MailId}", notificationEvent, mailId);
        }
    }
}
