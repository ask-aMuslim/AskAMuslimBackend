namespace AskAMuslimBackend.Domain.Entities;

public class QATag
{
    public Guid QAId { get; set; }
    public Guid TagId { get; set; }

    // Navigation
    public QA? QA { get; set; }
    public Tag? Tag { get; set; }
}
