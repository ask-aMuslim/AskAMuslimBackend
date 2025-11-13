using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class MeetingRequest : BaseAuditableEntity
{
    public Guid RequesterId { get; set; } // Student or NonMuslim (User.Id)
    public Guid? ScholarId { get; set; }   // Instructor or Preacher (User.Id) - set when accepted
    public MeetingTopic Topic { get; set; }
    public string Message { get; set; } = string.Empty;
    public Language Language { get; set; }
    public DateTime ScheduledAt { get; set; }
    public MeetingRequestStatus Status { get; set; } = MeetingRequestStatus.Pending;

    // Navigation (optional lightweight user references)
    public User? Requester { get; set; }
    public User? Scholar { get; set; }
}
