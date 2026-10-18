using System.Net;

namespace AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// HTML emails for support mails. Every user-provided value is HTML-encoded.
/// </summary>
public static class SupportMailEmailTemplates
{
    public record EmailContent(string Subject, string HtmlBody);

    public static EmailContent Receipt(string firstName, int number, string subject, string topic) => new(
        $"We received your inquiry (Mail #{number})",
        Layout("We received your inquiry", $@"
            <p>Hello {Encode(firstName)},</p>
            <p>Thank you for contacting us. Your inquiry <strong>&ldquo;{Encode(subject)}&rdquo;</strong> ({Encode(topic)}) has been received
               as <strong>Mail #{number}</strong>. We will respond as soon as possible.</p>
            <p>You can follow the conversation and reply from the <strong>Mails History</strong> screen in the app.</p>"));

    public static EmailContent NewMailForInbox(string requesterName, string? requesterEmail, int number, string subject, string topic, string message) => new(
        $"New support mail - {topic}: {subject}",
        Layout("New support mail", $@"
            {Details(requesterName, requesterEmail, number, topic, subject)}
            {Quote("Message", message)}
            <p>Reply or assign it from the admin dashboard.</p>"));

    public static EmailContent RequesterReplyForStaff(string requesterName, string? requesterEmail, int number, string subject, string topic, string message) => new(
        $"New reply on support mail #{number}: {subject}",
        Layout("The user replied", $@"
            {Details(requesterName, requesterEmail, number, topic, subject)}
            {Quote("Reply", message)}
            <p>Respond from the admin dashboard.</p>"));

    public static EmailContent AssignedToModerator(string moderatorFirstName, string requesterName, int number, string subject, string topic, string firstMessage) => new(
        $"Support mail assigned to you: {subject}",
        Layout("A support mail was assigned to you", $@"
            <p>Hello {Encode(moderatorFirstName)},</p>
            <p>You have been assigned this inquiry from <strong>{Encode(requesterName)}</strong> (Mail #{number}, {Encode(topic)}).</p>
            <p><strong>Subject:</strong> {Encode(subject)}</p>
            {Quote("Message", firstMessage)}
            <p>Respond from the admin dashboard.</p>"));

    public static EmailContent StaffReplyForRequester(string firstName, int number, string subject, string reply) => new(
        $"New reply on your inquiry (Mail #{number})",
        Layout("You have a new reply", $@"
            <p>Hello {Encode(firstName)},</p>
            <p>The support team replied to your inquiry <strong>&ldquo;{Encode(subject)}&rdquo;</strong>:</p>
            {Quote(null, reply)}
            <p>You can reply from the <strong>Mails History</strong> screen in the app.</p>"));

    public static string Encode(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty).Replace("\r\n", "<br/>").Replace("\n", "<br/>");

    private static string Details(string requesterName, string? requesterEmail, int number, string topic, string subject) => $@"
            <p style='margin: 6px 0;'><strong>From:</strong> {Encode(requesterName)}{(string.IsNullOrEmpty(requesterEmail) ? "" : $" &lt;{Encode(requesterEmail)}&gt;")}</p>
            <p style='margin: 6px 0;'><strong>Mail:</strong> #{number}</p>
            <p style='margin: 6px 0;'><strong>Topic:</strong> {Encode(topic)}</p>
            <p style='margin: 6px 0;'><strong>Subject:</strong> {Encode(subject)}</p>";

    private static string Quote(string? heading, string text) => $@"
            <div style='margin: 16px 0;'>
                {(heading == null ? "" : $"<h3 style='color: #1b5e20; font-size: 15px; margin: 0 0 8px;'>{Encode(heading)}:</h3>")}
                <div style='background-color: #f5f8f5; padding: 14px; border-left: 4px solid #1b6b35; border-radius: 3px;'>{Encode(text)}</div>
            </div>";

    private static string Layout(string title, string content) => $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px;'>
                    <h2 style='color: #1b6b35; border-bottom: 2px solid #1b6b35; padding-bottom: 10px;'>{Encode(title)}</h2>
                    {content}
                    <div style='margin-top: 20px; padding-top: 12px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 12px;'>
                        <p>Ask a Muslim support</p>
                    </div>
                </div>
            </body>
            </html>";
}
