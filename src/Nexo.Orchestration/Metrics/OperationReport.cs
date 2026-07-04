using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Metrics;

/// <summary>
/// Report for a single operation.
/// 
/// Contains:
/// - Operation name and execution count
/// - Duration statistics (total, average, min, max)
/// - Optional metadata dictionary
/// 
/// Used by PerformanceReport to report operation metrics.
/// </summary>
public sealed record OperationReport
{
    /// <summary>Name of the reported operation.</summary>
    public required string OperationName { get; init; }

    /// <summary>Number of recorded invocations.</summary>
    public required int Count { get; init; }

    /// <summary>Cumulative duration across all invocations.</summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>Mean invocation duration.</summary>
    public required TimeSpan AverageDuration { get; init; }

    /// <summary>Shortest observed invocation duration.</summary>
    public required TimeSpan MinDuration { get; init; }

    /// <summary>Longest observed invocation duration.</summary>
    public required TimeSpan MaxDuration { get; init; }

    /// <summary>Optional key-value metadata for the operation.</summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}
