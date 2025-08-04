using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Provides comprehensive authorization services with role-based access control and permission-based authorization
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Authorizes a user for a specific resource and action
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="resource">Resource to access</param>
    /// <param name="action">Action to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> AuthorizeAsync(UserInfo user, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes a user based on claims
    /// </summary>
    /// <param name="claims">User claims</param>
    /// <param name="resource">Resource to access</param>
    /// <param name="action">Action to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> AuthorizeAsync(IEnumerable<Claim> claims, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific role
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="role">Role to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role check result</returns>
    Task<RoleCheckResult> HasRoleAsync(UserInfo user, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="permission">Permission to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission check result</returns>
    Task<PermissionCheckResult> HasPermissionAsync(UserInfo user, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User roles</returns>
    Task<UserRolesResult> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permissions</returns>
    Task<UserPermissionsResult> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role assignment result</returns>
    Task<RoleAssignmentResult> AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="role">Role to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role removal result</returns>
    Task<RoleRemovalResult> RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role
    /// </summary>
    /// <param name="roleRequest">Role creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role creation result</returns>
    Task<RoleCreationResult> CreateRoleAsync(RoleCreationRequest roleRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role
    /// </summary>
    /// <param name="roleUpdateRequest">Role update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role update result</returns>
    Task<RoleUpdateResult> UpdateRoleAsync(RoleUpdateRequest roleUpdateRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role deletion result</returns>
    Task<RoleDeletionResult> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All roles</returns>
    Task<RolesListResult> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets role information
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Role information</returns>
    Task<RoleInfoResult> GetRoleInfoAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new permission
    /// </summary>
    /// <param name="permissionRequest">Permission creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission creation result</returns>
    Task<PermissionCreationResult> CreatePermissionAsync(PermissionCreationRequest permissionRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing permission
    /// </summary>
    /// <param name="permissionUpdateRequest">Permission update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission update result</returns>
    Task<PermissionUpdateResult> UpdatePermissionAsync(PermissionUpdateRequest permissionUpdateRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a permission
    /// </summary>
    /// <param name="permissionId">Permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission deletion result</returns>
    Task<PermissionDeletionResult> DeletePermissionAsync(string permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All permissions</returns>
    Task<PermissionsListResult> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns permissions to a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissions">Permissions to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission assignment result</returns>
    Task<PermissionAssignmentResult> AssignPermissionsToRoleAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes permissions from a role
    /// </summary>
    /// <param name="roleId">Role ID</param>
    /// <param name="permissions">Permissions to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Permission removal result</returns>
    Task<PermissionRemovalResult> RemovePermissionsFromRoleAsync(string roleId, List<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates dynamic permissions based on context
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="resource">Resource to access</param>
    /// <param name="action">Action to perform</param>
    /// <param name="context">Dynamic context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dynamic authorization result</returns>
    Task<DynamicAuthorizationResult> EvaluateDynamicPermissionAsync(UserInfo user, string resource, string action, Dictionary<string, object> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authorization configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization configuration</returns>
    Task<AuthorizationConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Authorization result
/// </summary>
public record AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public List<string> RequiredRoles { get; init; } = new();
    public List<string> RequiredPermissions { get; init; } = new();
    public List<string> UserRoles { get; init; } = new();
    public List<string> UserPermissions { get; init; } = new();
    public string? DenialReason { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Role check result
/// </summary>
public record RoleCheckResult
{
    public bool HasRole { get; init; }
    public string Role { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime CheckedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Permission check result
/// </summary>
public record PermissionCheckResult
{
    public bool HasPermission { get; init; }
    public string Permission { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime CheckedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// User roles result
/// </summary>
public record UserRolesResult
{
    public string UserId { get; init; } = string.Empty;
    public List<RoleInfo> Roles { get; init; } = new();
    public DateTime RetrievedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// User permissions result
/// </summary>
public record UserPermissionsResult
{
    public string UserId { get; init; } = string.Empty;
    public List<PermissionInfo> Permissions { get; init; } = new();
    public DateTime RetrievedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Role assignment result
/// </summary>
public record RoleAssignmentResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Role removal result
/// </summary>
public record RoleRemovalResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime RemovedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Role creation request
/// </summary>
public record RoleCreationRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Role creation result
/// </summary>
public record RoleCreationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Role update request
/// </summary>
public record RoleUpdateRequest
{
    public string RoleId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Role update result
/// </summary>
public record RoleUpdateResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Role deletion result
/// </summary>
public record RoleDeletionResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Roles list result
/// </summary>
public record RolesListResult
{
    public List<RoleInfo> Roles { get; init; } = new();
    public int TotalCount { get; init; }
    public DateTime RetrievedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Role information
/// </summary>
public record RoleInfo
{
    public string RoleId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = new();
    public int UserCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Role info result
/// </summary>
public record RoleInfoResult
{
    public bool IsSuccessful { get; init; }
    public RoleInfo? Role { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Permission creation request
/// </summary>
public record PermissionCreationRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Permission creation result
/// </summary>
public record PermissionCreationResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string PermissionId { get; init; } = string.Empty;
    public string PermissionName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Permission update request
/// </summary>
public record PermissionUpdateRequest
{
    public string PermissionId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Permission update result
/// </summary>
public record PermissionUpdateResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string PermissionId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Permission deletion result
/// </summary>
public record PermissionDeletionResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string PermissionId { get; init; } = string.Empty;
    public DateTime DeletedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Permissions list result
/// </summary>
public record PermissionsListResult
{
    public List<PermissionInfo> Permissions { get; init; } = new();
    public int TotalCount { get; init; }
    public DateTime RetrievedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Permission information
/// </summary>
public record PermissionInfo
{
    public string PermissionId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public int RoleCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Permission assignment result
/// </summary>
public record PermissionAssignmentResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public List<string> AssignedPermissions { get; init; } = new();
    public DateTime AssignedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Permission removal result
/// </summary>
public record PermissionRemovalResult
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public List<string> RemovedPermissions { get; init; } = new();
    public DateTime RemovedAt { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Dynamic authorization result
/// </summary>
public record DynamicAuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Dictionary<string, object> Context { get; init; } = new();
    public List<string> AppliedRules { get; init; } = new();
    public string? DenialReason { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Authorization configuration
/// </summary>
public record AuthorizationConfiguration
{
    public bool EnableRBAC { get; init; }
    public bool EnablePermissionBasedAuth { get; init; }
    public bool EnableDynamicPermissions { get; init; }
    public bool EnableRoleHierarchy { get; init; }
    public bool EnablePermissionCaching { get; init; }
    public TimeSpan CacheTimeout { get; init; }
    public List<string> DefaultRoles { get; init; } = new();
    public List<string> DefaultPermissions { get; init; } = new();
    public Dictionary<string, object> Rules { get; init; } = new();
} 