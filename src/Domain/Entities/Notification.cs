namespace AskAMuslimBackend.Domain.Entities;

public class Notification : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string NotificationType { get; set; } = "General"; // General, Course, Exam, System
}
