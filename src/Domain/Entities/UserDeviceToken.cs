namespace AskAMuslimBackend.Domain.Entities;

/// <summary>
/// An FCM registration token for one of a user's devices. A user can have several devices.
/// </summary>
public class UserDeviceToken : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; } = DevicePlatform.Unknown;
    public DateTimeOffset LastSeenAt { get; set; }

    public User? User { get; set; }
}
