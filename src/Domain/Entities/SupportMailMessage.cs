using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class SupportMailMessage : BaseAuditableEntity
{
    public Guid MailId { get; set; }
    public Guid SenderId { get; set; }
    public SupportMailSenderType SenderType { get; set; }
    public string Body { get; set; } = string.Empty;

    public SupportMail? Mail { get; set; }
    public User? Sender { get; set; }
}
