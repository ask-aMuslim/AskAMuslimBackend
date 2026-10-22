using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.MeetingRequests.Common;

public enum MeetingRequestNotificationEvent
{
    Created = 1,
    Assigned = 2,
    Accepted = 3,
    Rejected = 4,
    Cancelled = 5,
    Completed = 6,
    ResponseAdded = 7
}

/// <summary>
/// Queues meeting request notifications as background jobs. Call it after the change has been saved;
/// it returns immediately and never sends anything inside the request.
/// </summary>
public interface IMeetingRequestNotifier
{
    void RequestCreated(Guid requestId);
    void Assigned(Guid requestId, Guid moderatorId);

    /// <param name="actorId">The user who changed the status; for a cancellation it decides who gets told.</param>
    void StatusChanged(Guid requestId, MeetingInquiryRequestStatus status, Guid actorId);

    void ResponseAdded(Guid requestId);
}
