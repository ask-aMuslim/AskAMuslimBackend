using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries;

public class InquiryRequestDto
{
    public Guid Id { get; init; }
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public string? ModeratorName { get; init; }
    public Guid? ModeratorId { get; init; }
    public MeetingInquiryTopic Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<Language> Languages { get; init; } = new();
    public MeetingInquiryRequestStatus Status { get; init; }
    public string? Response { get; init; }
    public DateTimeOffset Created { get; init; }
}
