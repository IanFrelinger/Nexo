using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Multi-cloud deployment and scaling capabilities
/// </summary>
public partial interface IMultiCloudOrchestrator
{
    /// <summary>
    /// Deploys an application across multiple cloud providers
    /// </summary>
    /// <param name="deploymentRequest">Multi-cloud deployment request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-cloud deployment result</returns>
    Task<MultiCloudDeploymentResult> DeployAcrossProvidersAsync(MultiCloudDeploymentRequest deploymentRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployment status across all cloud providers
    /// </summary>
    /// <param name="deploymentId">Deployment identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deployment status across all providers</returns>
    Task<MultiCloudDeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales an application across multiple cloud providers
    /// </summary>
    /// <param name="scalingRequest">Multi-cloud scaling request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-cloud scaling result</returns>
    Task<MultiCloudScalingResult> ScaleAcrossProvidersAsync(MultiCloudScalingRequest scalingRequest, CancellationToken cancellationToken = default);
}

/// <summary>
/// Multi-cloud deployment request
/// </summary>
public record MultiCloudDeploymentRequest
{
    public string ApplicationName { get; init; } = string.Empty;
    public string ApplicationVersion { get; init; } = string.Empty;
    public List<string> TargetProviders { get; init; } = new();
    public DeploymentStrategy Strategy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
    public Dictionary<string, object> EnvironmentVariables { get; init; } = new();
    public List<string> Dependencies { get; init; } = new();
}

/// <summary>
/// Deployment strategy
/// </summary>
public enum DeploymentStrategy
{
    BlueGreen,
    Rolling,
    Canary,
    AllAtOnce,
    Failover
}

/// <summary>
/// Multi-cloud deployment result
/// </summary>
public record MultiCloudDeploymentResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime DeployedAt { get; init; }
    public List<ProviderDeploymentResult> ProviderResults { get; init; } = new();
    public Dictionary<string, string> Endpoints { get; init; } = new();
    public TimeSpan TotalDeploymentTime { get; init; }
}

/// <summary>
/// Provider deployment result
/// </summary>
public record ProviderDeploymentResult
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Endpoint { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime DeployedAt { get; init; }
    public TimeSpan DeploymentTime { get; init; }
}

/// <summary>
/// Multi-cloud deployment status
/// </summary>
public record MultiCloudDeploymentStatus
{
    public string DeploymentId { get; init; } = string.Empty;
    public string OverallStatus { get; init; } = string.Empty;
    public List<ProviderDeploymentStatus> ProviderStatuses { get; init; } = new();
    public DateTime LastUpdated { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
}

/// <summary>
/// Provider deployment status
/// </summary>
public record ProviderDeploymentStatus
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int HealthyInstances { get; init; }
    public int TotalInstances { get; init; }
    public double CpuUsage { get; init; }
    public double MemoryUsage { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Multi-cloud scaling request
/// </summary>
public record MultiCloudScalingRequest
{
    public string DeploymentId { get; init; } = string.Empty;
    public Dictionary<string, int> ProviderScaling { get; init; } = new();
    public ScalingStrategy Strategy { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
}

/// <summary>
/// Scaling strategy
/// </summary>
public enum ScalingStrategy
{
    Proportional,
    Absolute,
    Auto,
    Manual
}

/// <summary>
/// Multi-cloud scaling result
/// </summary>
public record MultiCloudScalingResult
{
    public string ScalingId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime ScaledAt { get; init; }
    public List<ProviderScalingResult> ProviderResults { get; init; } = new();
    public TimeSpan TotalScalingTime { get; init; }
}

/// <summary>
/// Provider scaling result
/// </summary>
public record ProviderScalingResult
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int PreviousInstances { get; init; }
    public int NewInstances { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ScaledAt { get; init; }
}
