using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS node pool information
/// </summary>
public record AKSNodePoolInfo
{
    public string Name { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string VmSize { get; init; } = string.Empty;
    public int NodeCount { get; init; }
    public int MinNodeCount { get; init; }
    public int MaxNodeCount { get; init; }
    public bool EnableAutoScaling { get; init; }
    public string OsType { get; init; } = string.Empty;
    public string OsDiskSizeGB { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
    public Dictionary<string, string> NodeLabels { get; init; } = new();
    public List<string> NodeTaints { get; init; } = new();
} 