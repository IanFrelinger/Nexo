using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS pod information
/// </summary>
public record AKSPodInfo
{
    public string Name { get; init; } = string.Empty;
    public string ClusterName { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    public string NodeName { get; init; } = string.Empty;
    public string PodIP { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public List<string> Containers { get; init; } = new();
    public Dictionary<string, string> Labels { get; init; } = new();
    public Dictionary<string, string> Annotations { get; init; } = new();
} 