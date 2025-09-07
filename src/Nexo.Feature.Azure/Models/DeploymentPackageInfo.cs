using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// Deployment package information
/// </summary>
public record DeploymentPackageInfo
{
    public string PackagePath { get; init; } = string.Empty;
    public long Size { get; init; }
    public string Runtime { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Checksum { get; init; } = string.Empty;
} 