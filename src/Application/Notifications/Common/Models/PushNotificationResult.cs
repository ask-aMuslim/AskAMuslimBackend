namespace AskAMuslimBackend.Application.Notifications.Common.Models;

public class PushNotificationResult
{
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public List<string> InvalidTokens { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    
    public bool IsSuccess => FailureCount == 0;
}
