using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Function app update information
/// </summary>
public record FunctionAppUpdateInfo
{
    public string ResourceGroupName { get; init; } = string.Empty;
    public string FunctionAppName { get; init; } = string.Empty;
    public string UpdateId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public long PackageSize { get; init; }
} 