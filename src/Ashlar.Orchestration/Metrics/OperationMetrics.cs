using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Abstractions.Agents;

namespace Ashlar.Orchestration.Metrics;

/// <summary>
/// Metrics for a specific operation.
/// 
/// Contains:
/// - Operation name
/// - Duration statistics (total, min, max, count)
/// - Optional metadata dictionary
/// 
/// Used internally by OrchestrationMetrics to track operation performance.
/// </summary>
internal sealed class OperationMetrics
{
    /// <summary>Name of the tracked operation.</summary>
    public required string OperationName { get; init; }

    /// <summary>Cumulative duration across all invocations.</summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>Number of recorded invocations.</summary>
    public int Count { get; set; }

    /// <summary>Shortest observed invocation duration.</summary>
    public TimeSpan MinDuration { get; set; }

    /// <summary>Longest observed invocation duration.</summary>
    public TimeSpan MaxDuration { get; set; }

    /// <summary>Optional key-value metadata for the operation.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
