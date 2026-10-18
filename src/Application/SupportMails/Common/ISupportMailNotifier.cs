namespace AskAMuslimBackend.Application.SupportMails.Common;

public enum SupportMailNotificationEvent
{
    Created = 1,
    RequesterReplied = 2,
    StaffReplied = 3,
    Assigned = 4,
    Closed = 5
}

/// <summary>
/// Queues support mail notifications as background jobs. Call it after the change has been saved;
/// it returns immediately and never sends anything inside the request.
/// </summary>
public interface ISupportMailNotifier
{
    void MailCreated(Guid mailId);
    void RequesterReplied(Guid mailId, Guid messageId);
    void StaffReplied(Guid mailId, Guid messageId);
    void Assigned(Guid mailId, Guid moderatorId);
    void Closed(Guid mailId);
}
