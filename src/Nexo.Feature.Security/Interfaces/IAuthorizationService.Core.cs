using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Core authorization functionality
/// </summary>
public partial interface IAuthorizationService
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
}
