using static AskAMuslimBackend.Infrastructure.Services.BackgroundJobs.SupportMailEmailTemplates;

namespace AskAMuslimBackend.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// HTML emails for meeting requests. Every user-provided value is HTML-encoded.
/// Emails to requesters take an optional <c>viewUrl</c>: a link to the request's page on the website.
/// </summary>
public static class MeetingRequestEmailTemplates
{
    public static EmailContent Receipt(string firstName, string topic, string scheduledAt, string? viewUrl = null) => new(
        "We received your meeting request",
        Layout("We received your meeting request", $@"
            <p>Hello {Encode(firstName)},</p>
            <p>Thank you for your meeting request about <strong>{Encode(topic)}</strong> on <strong>{Encode(scheduledAt)}</strong>.
               A scholar will review it and we will let you know as soon as there is an update.</p>
            {ViewButton(viewUrl)}"));

    public static EmailContent NewRequestForInbox(string requesterName, string? requesterEmail, string topic, string scheduledAt, string message) => new(
        $"New meeting request - {topic}",
        Layout("New meeting request", $@"
            {Details(requesterName, requesterEmail, topic, scheduledAt)}
            {Quote("Message", message)}
            <p>Review or assign it from the admin dashboard.</p>"));

    public static EmailContent AssignedToModerator(string moderatorFirstName, string requesterName, string topic, string scheduledAt, string message) => new(
        $"Meeting request assigned to you - {topic}",
        Layout("A meeting request was assigned to you", $@"
            <p>Hello {Encode(moderatorFirstName)},</p>
            <p>You have been assigned a meeting request from <strong>{Encode(requesterName)}</strong>.</p>
            <p style='margin: 6px 0;'><strong>Topic:</strong> {Encode(topic)}</p>
            <p style='margin: 6px 0;'><strong>Scheduled for:</strong> {Encode(scheduledAt)}</p>
            {Quote("Message", message)}
            <p>Accept or decline it from the admin dashboard.</p>"));

    public static EmailContent Accepted(string firstName, string topic, string scheduledAt, string? moderatorName, string? viewUrl = null) => new(
        "Your meeting request was accepted",
        Layout("Your meeting request was accepted", $@"
            <p>Hello {Encode(firstName)},</p>
            <p>Your meeting request about <strong>{Encode(topic)}</strong> on <strong>{Encode(scheduledAt)}</strong> was accepted{(string.IsNullOrWhiteSpace(moderatorName) ? "" : $" by <strong>{Encode(moderatorName)}</strong>")}.</p>
            {ViewButton(viewUrl)}"));

    public static EmailContent Rejected(string firstName, string topic, string scheduledAt, string? viewUrl = null) => new(
        "Your meeting request was declined",
        Layout("Your meeting request was declined", $@"
            <p>Hello {Encode(firstName)},</p>
            <p>Unfortunately we could not accept your meeting request about <strong>{Encode(topic)}</strong> on <strong>{Encode(scheduledAt)}</strong>.
               You are welcome to submit a new request for another time.</p>
            {ViewButton(viewUrl)}"));

    public static EmailContent Completed(string firstName, string topic, string scheduledAt, string? viewUrl = null) => new(
        "Your meeting was completed",
        Layout("Your meeting was completed", $@"
            <p>Hello {Encode(firstName)},</p>
            <p>Your meeting about <strong>{Encode(topic)}</strong> on <strong>{Encode(scheduledAt)}</strong> was marked as completed.
               Thank you for meeting with us.</p>
            {ViewButton(viewUrl)}"));

    public static EmailContent ResponseForRequester(string firstName, string topic, string scheduledAt, string response, string? viewUrl = null) => new(
        "You have a response to your meeting request",
        Layout("You have a new response", $@"
            <p>Hello {Encode(firstName)},</p>
            <p>The team responded to your meeting request about <strong>{Encode(topic)}</strong> on <strong>{Encode(scheduledAt)}</strong>:</p>
            {Quote(null, response)}
            {ViewButton(viewUrl)}"));

    private static string Details(string requesterName, string? requesterEmail, string topic, string scheduledAt) => $@"
            <p style='margin: 6px 0;'><strong>From:</strong> {Encode(requesterName)}{(string.IsNullOrEmpty(requesterEmail) ? "" : $" &lt;{Encode(requesterEmail)}&gt;")}</p>
            <p style='margin: 6px 0;'><strong>Topic:</strong> {Encode(topic)}</p>
            <p style='margin: 6px 0;'><strong>Scheduled for:</strong> {Encode(scheduledAt)}</p>";

    private static string ViewButton(string? url) => string.IsNullOrWhiteSpace(url) ? "" : $@"
            <p style='margin: 20px 0;'>
                <a href='{Encode(url)}' style='background-color: #1b6b35; color: #ffffff; padding: 10px 20px; border-radius: 5px; text-decoration: none; font-weight: bold;'>View your meeting</a>
            </p>";
}
