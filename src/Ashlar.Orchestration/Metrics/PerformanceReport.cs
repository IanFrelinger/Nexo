using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Abstractions.Agents;

namespace Ashlar.Orchestration.Metrics;

/// <summary>
/// Performance report containing all collected metrics.
/// 
/// Contains:
/// - Generation timestamp
/// - Lists of operation reports, agent reports, and trace spans
/// 
/// Produced by OrchestrationMetrics.GetPerformanceReport.
/// Used for performance analysis and reporting.
/// </summary>
public sealed record PerformanceReport
{
    /// <summary>UTC timestamp when the report was generated.</summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>Per-operation performance summaries.</summary>
    public required IReadOnlyList<OperationReport> Operations { get; init; }

    /// <summary>Per-agent execution summaries.</summary>
    public required IReadOnlyList<AgentReport> Agents { get; init; }

    /// <summary>Distributed trace span summaries.</summary>
    public required IReadOnlyList<SpanReport> Traces { get; init; }
}
