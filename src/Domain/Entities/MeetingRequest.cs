using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class MeetingRequest : BaseAuditableEntity
{
    public Guid RequesterId { get; set; } // Student or NonMuslim (User.Id)
    public Guid? ModeratorId { get; set; }   // Instructor or Preacher (User.Id) - set when accepted
    public MeetingInquiryTopic Topic { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Language> Languages { get; set; } = new();
    public DateTime ScheduledAt { get; set; }
    public MeetingInquiryRequestStatus Status { get; set; } = MeetingInquiryRequestStatus.Pending;

    // Admin response (optional)
    public string? Response { get; set; }

    // Navigation (optional lightweight user references)
    public User? Requester { get; set; }
    public User? Moderator { get; set; }
}
