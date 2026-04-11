using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AskAMuslimBackend.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public ReligiousStatus? ReligionStatus { get; set; }
    public string? Gender { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public string? FcmToken { get; set; }

    public ICollection<AppLanguage> Languages { get; set; } = new List<AppLanguage>();
}
