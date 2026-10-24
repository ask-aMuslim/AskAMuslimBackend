using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Common;

// ---- Requester (mobile) ----

public class MySupportMailListItemDto
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public MeetingInquiryTopic Topic { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public SupportMailStatus Status { get; init; }
    public bool HasUnreadReply { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastMessageAt { get; init; }
}

public class MySupportMailDetailDto
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public MeetingInquiryTopic Topic { get; init; }
    public string Subject { get; init; } = string.Empty;
    public SupportMailStatus Status { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastMessageAt { get; init; }
    public List<SupportMailMessageDto> Messages { get; init; } = new();
}

public class SupportMailEligibilityDto
{
    public bool CanCreate { get; init; }
    public DateTimeOffset? NextAllowedAt { get; init; }
}

// ---- Shared ----

public class SupportMailMessageDto
{
    public Guid Id { get; init; }
    public SupportMailSenderType SenderType { get; init; }
    /// <summary>Null for staff messages in the requester view (shown as "Support Team").</summary>
    public string? SenderName { get; init; }
    public string? SenderProfilePictureUrl { get; init; }
    public string Body { get; init; } = string.Empty;
    public DateTimeOffset Created { get; init; }
}

// ---- Staff (admin dashboard / moderators) ----

public class SupportMailListItemDto
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public string? RequesterEmail { get; init; }
    public Guid? AssignedModeratorId { get; init; }
    public string? AssignedModeratorName { get; init; }
    public MeetingInquiryTopic Topic { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Preview { get; init; } = string.Empty;
    public SupportMailStatus Status { get; init; }
    public int MessagesCount { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastMessageAt { get; init; }
}

public class SupportMailDetailDto
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public string? RequesterEmail { get; init; }
    public Guid? AssignedModeratorId { get; init; }
    public string? AssignedModeratorName { get; init; }
    public MeetingInquiryTopic Topic { get; init; }
    public string Subject { get; init; } = string.Empty;
    public SupportMailStatus Status { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastMessageAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public List<SupportMailMessageDto> Messages { get; init; } = new();
}
