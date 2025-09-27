using System;
using System.Collections.Generic;

namespace Nexo.Feature.Security.Interfaces;

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
