using System;
using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS cluster information
/// </summary>
public record AKSClusterInfo
{
    public string Name { get; init; } = string.Empty;
    public string ResourceGroup { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string KubernetesVersion { get; init; } = string.Empty;
    public string ApiServerUrl { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime LastModified { get; init; }
    public int NodeCount { get; init; }
    public string VmSize { get; init; } = string.Empty;
    public Dictionary<string, string> Tags { get; init; } = new();
    public List<string> NodePools { get; init; } = new();
} 