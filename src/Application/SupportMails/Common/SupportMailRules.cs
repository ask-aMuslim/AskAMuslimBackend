using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Common;

public static class SupportMailRules
{
    public static readonly TimeSpan NewMailCooldown = TimeSpan.FromHours(1);

    public const int SubjectMaxLength = 200;
    public const int ExplanationMaxLength = 500;
    public const int ReplyMaxLength = 4000;
    public const int PreviewLength = 150;

    /// <summary>
    /// Staff may access a mail when they are its assigned moderator or hold the given inquiry permission.
    /// </summary>
    public static async Task<bool> CanStaffAccessAsync(
        SupportMail mail,
        Guid userId,
        Permission permission,
        IPermissionService permissionService,
        CancellationToken cancellationToken)
    {
        if (mail.AssignedModeratorId == userId)
        {
            return true;
        }

        return await permissionService.HasPermissionAsync(userId.ToString(), permission, cancellationToken);
    }

    /// <summary>
    /// Appends a message and moves the mail to the status implied by who sent it.
    /// </summary>
    public static SupportMailMessage AddMessage(SupportMail mail, Guid senderId, SupportMailSenderType senderType, string body, DateTimeOffset now)
    {
        var message = new SupportMailMessage
        {
            MailId = mail.Id,
            SenderId = senderId,
            SenderType = senderType,
            Body = body.Trim(),
        };

        mail.Messages.Add(message);
        mail.LastMessageAt = now;
        mail.LastMessageSenderType = senderType;
        mail.Status = senderType == SupportMailSenderType.Requester
            ? SupportMailStatus.AwaitingResponse
            : SupportMailStatus.Responded;
        mail.ClosedAt = null;

        return message;
    }
}
