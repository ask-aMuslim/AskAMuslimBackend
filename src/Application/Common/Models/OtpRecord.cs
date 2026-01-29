namespace AskAMuslimBackend.Application.Common.Models;

public class OtpRecord
{
    public string Email { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; } = 0;
}
