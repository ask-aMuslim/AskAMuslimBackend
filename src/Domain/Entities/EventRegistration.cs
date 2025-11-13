namespace AskAMuslimBackend.Domain.Entities;

public class EventRegistration : BaseAuditableEntity
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string? QuestionText { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Pending;

    // Navigation
    public Event? Event { get; set; }
    public User? User { get; set; }
}
