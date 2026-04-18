namespace AskAMuslimBackend.Domain.Entities;

public class Form : BaseAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublished { get; set; }

    public ICollection<FormField> Fields { get; set; } = new HashSet<FormField>();
    public ICollection<FormSubmission> Submissions { get; set; } = new HashSet<FormSubmission>();
}
