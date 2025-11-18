namespace AskAMuslimBackend.Domain.Entities;

public class Preacher : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? LinkedInProfile { get; set; }
    public int YearsOfExperience { get; set; }
    public bool IsVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public decimal Rating { get; set; } = 0;
    public int TotalReviews { get; set; } = 0;
    public string? Certifications { get; set; }
    public string? Organization { get; set; }
    public bool CanAccessStudentProfiles { get; set; } = true;
}
