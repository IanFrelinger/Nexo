using System.Collections.Generic;

namespace Nexo.Feature.Azure.Models;

/// <summary>
/// AKS cluster configuration
/// </summary>
public record AKSClusterConfiguration
{
    public string? KubernetesVersion { get; init; }
    public Dictionary<string, string> Tags { get; init; } = new();
    public Dictionary<string, string> AddOnProfiles { get; init; } = new();
    public Dictionary<string, object> NetworkProfile { get; init; } = new();
    public Dictionary<string, object> IdentityProfile { get; init; } = new();
} 