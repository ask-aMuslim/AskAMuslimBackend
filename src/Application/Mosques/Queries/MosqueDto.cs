using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Mosques.Queries;

public class MosqueDto
{
    public Guid Id { get; set; }
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
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public double? DistanceInKm { get; set; }
}

public static class MosqueMappings
{
    public static IQueryable<MosqueDto> ProjectToMosqueDto(this IQueryable<Mosque> query)
    {
        return query.Select(m => new MosqueDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Address = m.Address,
            Country = m.Country,
            Governorate = m.Governorate,
            City = m.City,
            District = m.District,
            Latitude = m.Latitude,
            Longitude = m.Longitude,
            GoogleMapsUrl = m.GoogleMapsUrl,
            PhoneNumber = m.PhoneNumber,
            Email = m.Email,
            WebsiteUrl = m.WebsiteUrl,
            ImamName = m.ImamName,
            HasWomenPrayerArea = m.HasWomenPrayerArea,
            HasFridayKhutbah = m.HasFridayKhutbah,
            MainImageUrl = m.MainImageUrl,
            IsActive = m.IsActive,
            IsVerified = m.IsVerified,
            CreatedAt = m.Created,
            UpdatedAt = m.LastModified
        });
    }
}
