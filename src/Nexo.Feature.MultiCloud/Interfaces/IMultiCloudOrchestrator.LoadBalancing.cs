using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Multi-cloud load balancing capabilities
/// </summary>
public partial interface IMultiCloudOrchestrator
{
    /// <summary>
    /// Load balances traffic across cloud providers
    /// </summary>
    /// <param name="loadBalancingRequest">Load balancing request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Load balancing result</returns>
    Task<MultiCloudLoadBalancingResult> LoadBalanceTrafficAsync(MultiCloudLoadBalancingRequest loadBalancingRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets load balancing configuration
    /// </summary>
    /// <param name="loadBalancerId">Load balancer identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Load balancing configuration</returns>
    Task<MultiCloudLoadBalancingConfig> GetLoadBalancingConfigAsync(string loadBalancerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Multi-cloud load balancing request
/// </summary>
public record MultiCloudLoadBalancingRequest
{
    public string LoadBalancerName { get; init; } = string.Empty;
    public List<string> Providers { get; init; } = new();
    public LoadBalancingAlgorithm Algorithm { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public List<string> HealthChecks { get; init; } = new();
}

/// <summary>
/// Load balancing algorithm
/// </summary>
public enum LoadBalancingAlgorithm
{
    RoundRobin,
    LeastConnections,
    WeightedRoundRobin,
    LeastResponseTime,
    IPHash
}

/// <summary>
/// Multi-cloud load balancing result
/// </summary>
public record MultiCloudLoadBalancingResult
{
    public string LoadBalancerId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Endpoint { get; init; } = string.Empty;
    public List<ProviderLoadBalancerConfig> ProviderConfigs { get; init; } = new();
}

/// <summary>
/// Provider load balancer configuration
/// </summary>
public record ProviderLoadBalancerConfig
{
    public string ProviderName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public int Weight { get; init; }
    public bool IsHealthy { get; init; }
    public double ResponseTime { get; init; }
}

/// <summary>
/// Multi-cloud load balancing configuration
/// </summary>
public record MultiCloudLoadBalancingConfig
{
    public string LoadBalancerId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public LoadBalancingAlgorithm Algorithm { get; init; }
    public List<ProviderLoadBalancerConfig> ProviderConfigs { get; init; } = new();
    public Dictionary<string, object> Configuration { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}
