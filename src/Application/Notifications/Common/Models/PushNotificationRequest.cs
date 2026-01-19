namespace AskAMuslimBackend.Application.Notifications.Common.Models;

public class PushNotificationRequest
{
    public required string Title { get; init; }
    public required string Body { get; init; }
    public string? ImageUrl { get; init; }
    public string? ActionUrl { get; init; }
    public Dictionary<string, string>? CustomData { get; init; }
}
