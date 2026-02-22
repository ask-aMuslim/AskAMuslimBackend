using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Reports.Queries;

public class ReportDto
{
    public Guid Id { get; init; }
    public ReportType ReportType { get; init; }
    public Guid ReportedContentId { get; init; }
    public ReportReason Reason { get; init; }
    public string Description { get; init; } = string.Empty;
    public ReportStatus Status { get; init; }
    public Guid ReportedByUserId { get; init; }
    public string? ReportedByUserName { get; init; }
    public string? ReportedByUserEmail { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public string? ReviewedByUserName { get; init; }
    public string? ReviewNotes { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset? LastModified { get; init; }
}