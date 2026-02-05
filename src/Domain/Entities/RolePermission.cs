namespace AskAMuslimBackend.Domain.Entities;

public class RolePermission
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public Permission Permissions { get; set; } = Permission.None;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedByUserId { get; set; }
    public string? Notes { get; set; }
}
