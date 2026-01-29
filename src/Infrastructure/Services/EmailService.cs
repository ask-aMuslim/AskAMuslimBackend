using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace AskAMuslimBackend.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        
        // Load email settings from external configuration file
        var emailConfigPath = configuration["EmailService:ConfigPath"];
        if (string.IsNullOrEmpty(emailConfigPath))
        {
            throw new InvalidOperationException("Email service configuration path not found in appsettings.");
        }

        var fullPath = Path.IsPathRooted(emailConfigPath) 
            ? emailConfigPath 
            : Path.Combine(AppContext.BaseDirectory, emailConfigPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Email configuration file not found at: {fullPath}");
        }

        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(fullPath, optional: false, reloadOnChange: true);

        var emailConfig = configBuilder.Build();
        _emailSettings = emailConfig.GetSection("EmailSettings").Get<EmailSettings>() 
            ?? throw new InvalidOperationException("EmailSettings section not found in email configuration file.");
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        await SendEmailAsync(new[] { to }, subject, body, isHtml);
    }

    public async Task SendEmailAsync(string[] to, string subject, string body, bool isHtml = true)
    {
        try
        {
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var recipient in to)
            {
                mailMessage.To.Add(recipient);
            }

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", to));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", to));
            throw;
        }
    }

    public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, string attachmentPath, bool isHtml = true)
    {
        try
        {
            if (!File.Exists(attachmentPath))
            {
                throw new FileNotFoundException($"Attachment file not found: {attachmentPath}");
            }

            using var smtpClient = CreateSmtpClient();
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            mailMessage.To.Add(to);
            mailMessage.Attachments.Add(new Attachment(attachmentPath));

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email with attachment sent successfully to {Recipient}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {Recipient}", to);
            throw;
        }
    }

    public async Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true)
    {
        var tasks = recipients.Select(recipient => SendEmailAsync(recipient, subject, body, isHtml));
        await Task.WhenAll(tasks);
    }

    private SmtpClient CreateSmtpClient()
    {
        return new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
            Timeout = _emailSettings.TimeoutSeconds * 1000
        };
    }
}
