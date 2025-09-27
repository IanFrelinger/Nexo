using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Role management functionality
/// </summary>
public partial interface IAuthorizationService
{
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
}
