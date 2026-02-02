using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Permissions;

public class PermissionDto
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public long Value { get; init; }
    public bool IsGranted { get; init; }
}

public class UserPermissionsDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public Permission Permissions { get; init; }
    public List<PermissionDto> PermissionList { get; init; } = new();
    public DateTime? GrantedDate { get; init; }
    public string? GrantedBy { get; init; }
}

public class PermissionCategoryDto
{
    public string Category { get; init; } = string.Empty;
    public List<PermissionDto> Permissions { get; init; } = new();
}
