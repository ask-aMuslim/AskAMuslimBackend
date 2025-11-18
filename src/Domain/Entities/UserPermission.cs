using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class UserPermission : BaseEntity
{
    public Guid UserId { get; set; }
    public Permission Permissions { get; set; } = Permission.None;
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    public Guid? GrantedByUserId { get; set; }
    public string? Notes { get; set; }
}
