using AskAMuslimBackend.Application.MeetingRequests.Common;
using AskAMuslimBackend.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Hangfire-backed <see cref="IMeetingRequestNotifier"/>. Jobs carry ids only and reload data when they run.
/// </summary>
public class MeetingRequestNotifier : IMeetingRequestNotifier
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<MeetingRequestNotifier> _logger;

    public MeetingRequestNotifier(IBackgroundJobClient backgroundJobClient, ILogger<MeetingRequestNotifier> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public void RequestCreated(Guid requestId) =>
        Enqueue(MeetingRequestNotificationEvent.Created, requestId, null);

    public void Assigned(Guid requestId, Guid moderatorId) =>
        Enqueue(MeetingRequestNotificationEvent.Assigned, requestId, moderatorId);

    public void StatusChanged(Guid requestId, MeetingInquiryRequestStatus status, Guid actorId)
    {
        switch (status)
        {
            case MeetingInquiryRequestStatus.Accepted:
                Enqueue(MeetingRequestNotificationEvent.Accepted, requestId, actorId);
                break;
            case MeetingInquiryRequestStatus.Rejected:
                Enqueue(MeetingRequestNotificationEvent.Rejected, requestId, actorId);
                break;
            case MeetingInquiryRequestStatus.Cancelled:
                Enqueue(MeetingRequestNotificationEvent.Cancelled, requestId, actorId);
                break;
            case MeetingInquiryRequestStatus.Completed:
                Enqueue(MeetingRequestNotificationEvent.Completed, requestId, actorId);
                break;
            // Moving back to Pending is internal bookkeeping; nobody needs to hear about it.
        }
    }

    public void ResponseAdded(Guid requestId) =>
        Enqueue(MeetingRequestNotificationEvent.ResponseAdded, requestId, null);

    private void Enqueue(MeetingRequestNotificationEvent notificationEvent, Guid requestId, Guid? relatedId)
    {
        try
        {
            var eventId = Guid.NewGuid();
            _backgroundJobClient.Enqueue<MeetingRequestNotificationJobs>(
                jobs => jobs.DispatchAsync(notificationEvent, requestId, relatedId, eventId, CancellationToken.None));
        }
        catch (Exception ex)
        {
            // The change is already saved; a queueing failure must not fail the request.
            _logger.LogError(ex, "Failed to queue {Event} notification for meeting request {RequestId}", notificationEvent, requestId);
        }
    }
}
