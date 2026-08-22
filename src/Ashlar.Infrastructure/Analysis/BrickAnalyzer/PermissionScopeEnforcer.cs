using Ashlar.Core.Application.Analysis.Models;
using Ashlar.Core.Application.Analysis.Ports;

namespace Ashlar.Infrastructure.Analysis.BrickAnalyzer;

/// <summary>
/// Enforces permission boundaries: application scope cannot perform OS-level operations.
/// </summary>
public sealed class PermissionScopeEnforcer : IPermissionScopeEnforcer
{
    private static readonly HashSet<string> OsLevelOperationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ProcessStart",
        "ProcessKill",
        "RawFileSystem",
        "RegistryAccess",
        "RawNetwork",
    };

    /// <inheritdoc />
    public bool IsAllowed(PermissionScope scope, string operationType, string? target = null)
    {
        if (scope == PermissionScope.OperatingSystem)
            return true;

        if (scope == PermissionScope.Application && OsLevelOperationTypes.Contains(operationType))
            return false;

        return true;
    }
}
