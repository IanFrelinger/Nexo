using System;
using System.Collections.Generic;

namespace Nexo.Feature.Security.Interfaces;

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
