using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Common.Interfaces;

public interface IPermissionService
{
    Task<Permission> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string userId, Permission permission, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(string userId, Permission[] permissions, CancellationToken cancellationToken = default);
    Task<bool> HasAllPermissionsAsync(string userId, Permission[] permissions, CancellationToken cancellationToken = default);
    Task SetUserPermissionsAsync(string userId, Permission permissions, string? grantedByUserId = null, string? notes = null, CancellationToken cancellationToken = default);
    Task AddPermissionsAsync(string userId, Permission permissions, string? grantedByUserId = null, CancellationToken cancellationToken = default);
    Task RemovePermissionsAsync(string userId, Permission permissions, CancellationToken cancellationToken = default);
    Task<Permission> GetDefaultPermissionsForRoleAsync(string role);
    void InvalidateRolePermissionsCache(string role);
}
