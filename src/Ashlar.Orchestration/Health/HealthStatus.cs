using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Agents;
using Ashlar.Orchestration.Agents;
using System.Collections.Concurrent;

namespace Ashlar.Orchestration.Health;

/// <summary>
/// Health status of a component.
/// 
/// Contains:
/// - Component name and health status (healthy/unhealthy)
/// - Status string and optional message
/// - Timestamp and optional details dictionary
/// 
/// Used by IHealthCheck to report component health.
/// </summary>
public sealed record HealthStatus
{
    /// <summary>Component name being checked.</summary>
    public required string Name { get; init; }

    /// <summary>Whether the component is healthy.</summary>
    public required bool IsHealthy { get; init; }

    /// <summary>Short status label for display.</summary>
    public required string Status { get; init; }

    /// <summary>Optional detail message when unhealthy.</summary>
    public string? Message { get; init; }

    /// <summary>UTC timestamp when the check was performed.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Additional diagnostic key-value details.</summary>
    public IReadOnlyDictionary<string, object> Details { get; init; } = new Dictionary<string, object>();
}
