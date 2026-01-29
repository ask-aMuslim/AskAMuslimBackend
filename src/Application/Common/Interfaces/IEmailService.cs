namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailAsync(string[] to, string subject, string body, bool isHtml = true);
    Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentPath, bool isHtml = true);
    Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true);
}
