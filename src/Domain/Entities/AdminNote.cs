namespace AskAMuslimBackend.Domain.Entities;

public class AdminNote : BaseAuditableEntity
{
    public Guid StudentId { get; set; }
    public Guid AdminUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; } = false;
    
    // Navigation properties
    public Student? Student { get; set; }
}
