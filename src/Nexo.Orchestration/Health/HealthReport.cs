using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Agents;
using Nexo.Orchestration.Agents;
using System.Collections.Concurrent;

namespace Nexo.Orchestration.Health;

/// <summary>
/// Overall health report.
/// 
/// Contains:
/// - Overall healthy status (true if all components are healthy)
/// - Timestamp and list of individual component statuses
/// 
/// Produced by HealthCheckService.CheckAllAsync.
/// </summary>
public sealed record HealthReport
{
    /// <summary>Whether all components reported healthy.</summary>
    public required bool OverallHealthy { get; init; }

    /// <summary>UTC timestamp when the report was generated.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Individual component health statuses.</summary>
    public required IReadOnlyList<HealthStatus> Statuses { get; init; }
}
