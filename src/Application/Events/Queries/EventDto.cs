using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Events.Queries;

public class EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? SpeakerName { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public EventStatus Status { get; init; }
    public string? MeetingLink { get; init; }
    public bool IsPublished { get; init; }
    public int TotalRegistrations { get; init; }
}
