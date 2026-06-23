namespace AskAMuslimBackend.Domain.Entities;

public class Mosque : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? ImamName { get; set; }
    public bool HasWomenPrayerArea { get; set; }
    public bool HasFridayKhutbah { get; set; }
    public string? MainImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }
}
