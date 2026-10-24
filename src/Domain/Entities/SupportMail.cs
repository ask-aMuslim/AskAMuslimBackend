using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class SupportMail : BaseAuditableEntity
{
    public int Number { get; set; } // Sequential per requester ("Mail #4")
    public Guid RequesterId { get; set; }
    public Guid? AssignedModeratorId { get; set; } // Instructor, Preacher or Admin (User.Id)
    public MeetingInquiryTopic Topic { get; set; }
    public string Subject { get; set; } = string.Empty; // One-sentence summary of the question
    public SupportMailStatus Status { get; set; } = SupportMailStatus.AwaitingResponse;
    public DateTimeOffset LastMessageAt { get; set; }
    public SupportMailSenderType LastMessageSenderType { get; set; } = SupportMailSenderType.Requester;
    public DateTimeOffset? RequesterLastReadAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public User? Requester { get; set; }
    public User? AssignedModerator { get; set; }
    public List<SupportMailMessage> Messages { get; set; } = new();
}
