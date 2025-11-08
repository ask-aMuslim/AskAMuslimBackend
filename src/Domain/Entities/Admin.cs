namespace AskAMuslimBackend.Domain.Entities;

public class Admin : BaseEntity
{
    public Guid UserId { get; set; }
    public string? Department { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsSuperAdmin { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }
    
    // Permissions (can be extended with a separate Permissions entity if needed)
    public bool CanManageUsers { get; set; } = true;
    public bool CanManageCourses { get; set; } = true;
    public bool CanManageContent { get; set; } = true;
    public bool CanViewReports { get; set; } = true;
    public bool CanManageRoles { get; set; } = false;
}
