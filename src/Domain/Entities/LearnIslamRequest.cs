using AskAMuslimBackend.Domain.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class LearnIslamRequest : BaseAuditableEntity
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Country { get; set; } = null!;
    public Language PreferredLanguage { get; set; }
    public LearnIslamKnowledgeLevel KnowledgeLevel { get; set; }
    public string? Message { get; set; }
    public string? CurrentReligion { get; set; }
    
    // Admin fields
    public LearnIslamRequestStatus Status { get; set; } = LearnIslamRequestStatus.Pending;
    public string? Label { get; set; }
}