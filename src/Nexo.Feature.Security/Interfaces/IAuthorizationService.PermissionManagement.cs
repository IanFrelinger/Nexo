using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Permission management functionality
/// </summary>
public partial interface IAuthorizationService
{
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
}
