using System;
using System.Collections.Generic;

namespace Nexo.Feature.Security.Interfaces;

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
