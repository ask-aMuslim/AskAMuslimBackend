namespace AskAMuslimBackend.Application.Students.Queries;

public class StudentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly EnrollmentDate { get; set; }
    public bool IsActive { get; set; }
    public int TotalEnrollments { get; set; }
    public bool HasProfile { get; set; }
}
