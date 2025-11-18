namespace AskAMuslimBackend.Application.Admins.Queries;

public class AdminDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
