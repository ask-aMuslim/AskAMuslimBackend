using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries;

public class MeetingRequestDto
{
    public Guid Id { get; init; }
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public string? ModeratorName { get; init; }
    public Guid? ModeratorId { get; init; }
    public MeetingInquiryTopic Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<Language> Languages { get; init; } = new();
    public DateTime ScheduledAt { get; init; }
    public MeetingInquiryRequestStatus Status { get; init; }
    public string? Response { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
