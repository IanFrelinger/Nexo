using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS cluster creation information
/// </summary>
public record AKSClusterCreationInfo
{
    public string ResourceGroupName { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Region { get; init; } = string.Empty;
    public string KubernetesVersion { get; init; } = string.Empty;
    public int NodeCount { get; init; }
    public string VmSize { get; init; } = string.Empty;
    public string ApiServerUrl { get; init; } = string.Empty;
} 