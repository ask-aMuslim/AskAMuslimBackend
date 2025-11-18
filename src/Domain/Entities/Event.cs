namespace AskAMuslimBackend.Domain.Entities;

public class Event : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SpeakerName { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? MeetingLink { get; set; }
    public bool IsPublished { get; set; }

    // Navigation
    public ICollection<EventRegistration> Registrations { get; set; } = new HashSet<EventRegistration>();
}
