using AskAMuslimBackend.Domain.Common;

namespace AskAMuslimBackend.Domain.Entities;

public class NewsletterSubscription : BaseAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? UnsubscribedAt { get; set; }
}