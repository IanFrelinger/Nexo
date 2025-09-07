using System;

namespace Nexo.Feature.AI.Models;

/// <summary>
/// Health status of a model provider
/// </summary>
public record ProviderHealthStatus
{
    /// <summary>
    /// Provider name
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;
    
    /// <summary>
    /// Overall health status
    /// </summary>
    public HealthStatus Status { get; init; } = HealthStatus.Unknown;
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; init; }
    
    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime LastChecked { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Error message if unhealthy
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Additional health metrics
    /// </summary>
    public Dictionary<string, object> Metrics { get; init; } = new();
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Health status is unknown
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Provider is healthy
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Provider is degraded
    /// </summary>
    Degraded,
    
    /// <summary>
    /// Provider is unhealthy
    /// </summary>
    Unhealthy
}
