using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.StudentProfiles;

public class StudentProfileDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid UserId { get; set; }

    // User Information
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Gender { get; set; }
    public List<Language> Languages { get; set; } = new();
    public ReligiousStatus ReligiousStatus { get; set; }

    // Basic Profile Information
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    // Religious Status Information
    public bool IsMuslim { get; set; }
    public bool IsNewMuslim { get; set; }

    // For New Muslims (Reverts) only
    public DateOnly? DateOfIslamConversion { get; set; }
    public string? OldReligion { get; set; }
    public string? ReasonOfReligion { get; set; }
    public string? ReasonForConversion { get; set; }
    public bool IsProfileCompleted { get; set; }
}
