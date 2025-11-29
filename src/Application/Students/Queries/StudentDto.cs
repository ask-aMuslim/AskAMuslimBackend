namespace AskAMuslimBackend.Application.Students.Queries;

public class StudentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public bool IsActive { get; set; }
    public int TotalEnrollments { get; set; }
}
