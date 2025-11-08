namespace AskAMuslimBackend.Application.Instructors.Queries;

public class InstructorDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Website { get; set; }
    public string? LinkedInProfile { get; set; }
    public int YearsOfExperience { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedDate { get; set; }
    public decimal Rating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalCourses { get; set; }
}
