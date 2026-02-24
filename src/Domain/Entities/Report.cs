using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class Report : BaseAuditableEntity
{
    public ReportType ReportType { get; set; }
    public Guid? ReportedContentId { get; set; }
    public ReportReason Reason { get; set; }
    public string Description { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public Guid ReportedByUserId { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }

    // Navigation properties
    public User? ReportedBy { get; set; }

    public User? ReviewedBy { get; set; }
}
