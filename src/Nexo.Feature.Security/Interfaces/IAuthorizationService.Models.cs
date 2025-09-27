using System;
using System.Collections.Generic;

namespace Nexo.Feature.Security.Interfaces;

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
/// User information
/// </summary>
public record UserInfo
{
    public string UserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public List<string> Permissions { get; init; } = new();
    public Dictionary<string, object> Metadata { get; init; } = new();
}
