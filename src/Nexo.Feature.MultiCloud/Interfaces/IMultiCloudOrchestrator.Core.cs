using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.MultiCloud.Interfaces;

/// <summary>
/// Core multi-cloud orchestration capabilities
/// </summary>
public partial interface IMultiCloudOrchestrator
{
    /// <summary>
    /// Gets the list of available cloud providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available cloud providers</returns>
    Task<List<CloudProviderInfo>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to all configured cloud providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Connectivity test results for all providers</returns>
    Task<MultiCloudConnectivityResult> TestAllProvidersConnectivityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors health across all cloud providers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status across all providers</returns>
    Task<MultiCloudHealthStatus> MonitorHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cloud provider information
/// </summary>
public record CloudProviderInfo
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool IsConnected { get; init; }
    public List<string> SupportedServices { get; init; } = new();
    public Dictionary<string, object> Configuration { get; init; } = new();
}

/// <summary>
/// Multi-cloud connectivity test result
/// </summary>
public record MultiCloudConnectivityResult
{
    public DateTime TestedAt { get; init; }
    public List<ProviderConnectivityResult> ProviderResults { get; init; } = new();
    public bool AllProvidersConnected { get; init; }
    public TimeSpan TotalTestDuration { get; init; }
}

/// <summary>
/// Provider connectivity result
/// </summary>
public record ProviderConnectivityResult
{
    public string ProviderName { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public long LatencyMs { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public DateTime TestedAt { get; init; }
}

/// <summary>
/// Multi-cloud health status
/// </summary>
public record MultiCloudHealthStatus
{
    public DateTime CheckedAt { get; init; }
    public string OverallStatus { get; init; } = string.Empty;
    public List<ProviderHealthStatus> ProviderStatuses { get; init; } = new();
    public List<HealthAlert> Alerts { get; init; } = new();
}

/// <summary>
/// Provider health status
/// </summary>
public record ProviderHealthStatus
{
    public string ProviderName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double Uptime { get; init; }
    public double ResponseTime { get; init; }
    public List<string> Issues { get; init; } = new();
    public DateTime LastChecked { get; init; }
}

/// <summary>
/// Health alert
/// </summary>
public record HealthAlert
{
    public string AlertId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime RaisedAt { get; init; }
    public bool IsResolved { get; init; }
}
