using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS service information
/// </summary>
public record AKSServiceInfo
{
    public string Name { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Port { get; init; }
    public int TargetPort { get; init; }
    public string ClusterIP { get; init; } = string.Empty;
    public string ExternalIP { get; init; } = string.Empty;
    public int Replicas { get; init; }
    public int AvailableReplicas { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public Dictionary<string, string> Labels { get; init; } = new();
    public Dictionary<string, string> Annotations { get; init; } = new();
} 