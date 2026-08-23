using Ashlar.Core.Application.Analysis.Models;

namespace Ashlar.Core.Application.Analysis.Ports;

/// <summary>
/// Enforces permission boundaries: application-scope bricks cannot perform OS-level operations.
/// Unapproved changes are blocked before execution.
/// </summary>
public interface IPermissionScopeEnforcer
{
    /// <summary>
    /// Check whether the operation is allowed in the given scope.
    /// </summary>
    /// <param name="scope">The brick/component's declared scope.</param>
    /// <param name="operationType">e.g. ProcessStart, FileWrite, NetworkCall.</param>
    /// <param name="target">Optional target (e.g. file path, URL).</param>
    /// <returns>True if allowed; false to block.</returns>
    bool IsAllowed(PermissionScope scope, string operationType, string? target = null);
}
