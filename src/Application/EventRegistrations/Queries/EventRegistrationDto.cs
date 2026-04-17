using System.Linq.Expressions;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries;

public class EventRegistrationDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? QuestionText { get; init; }
    public EventRegistrationStatus Status { get; init; }
}
