using AskAMuslimBackend.Application.Notifications.Commands.SendNotification;
using AskAMuslimBackend.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.SupportMails.Common;

/// <summary>
/// Push + in-app + email notifications for support mails. Failures are logged and never fail the calling command.
/// </summary>
public static class SupportMailNotifications
{
    public const string NotificationType = "support_mail";

    public static Task NotifyRequesterOfReplyAsync(ISender sender, ILogger logger, SupportMail mail, CancellationToken cancellationToken) =>
        SendAsync(sender, logger, mail.RequesterId, mail,
            $"New reply on Mail #{mail.Number}",
            $"The support team replied to your inquiry \"{mail.Subject}\".",
            cancellationToken);

    public static Task NotifyModeratorOfAssignmentAsync(ISender sender, ILogger logger, SupportMail mail, Guid moderatorId, CancellationToken cancellationToken) =>
        SendAsync(sender, logger, moderatorId, mail,
            "New inquiry assigned to you",
            $"You have been assigned the inquiry \"{mail.Subject}\".",
            cancellationToken);

    private static async Task SendAsync(ISender sender, ILogger logger, Guid userId, SupportMail mail, string title, string message, CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(new SendNotificationCommand
            {
                UserId = userId,
                Title = title,
                Message = message,
                CustomData = new Dictionary<string, string>
                {
                    ["type"] = NotificationType,
                    ["mailId"] = mail.Id.ToString()
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send support mail notification for mail {MailId} to user {UserId}", mail.Id, userId);
        }
    }
}
