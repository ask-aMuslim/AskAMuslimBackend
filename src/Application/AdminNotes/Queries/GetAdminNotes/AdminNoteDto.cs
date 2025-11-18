namespace AskAMuslimBackend.Application.AdminNotes.Queries.GetAdminNotes;

public class AdminNoteDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid AdminUserId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
