using System;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS service deployment information
/// </summary>
public record AKSServiceDeploymentInfo
{
    public string ClusterName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DeployedAt { get; init; }
    public string ImageName { get; init; } = string.Empty;
    public int Replicas { get; init; }
    public int Port { get; init; }
    public int TargetPort { get; init; }
    public string ServiceUrl { get; init; } = string.Empty;
} 