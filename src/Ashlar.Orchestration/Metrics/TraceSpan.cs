using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Abstractions.Agents;

namespace Ashlar.Orchestration.Metrics;

/// <summary>
/// Trace span for distributed tracing.
/// 
/// Contains:
/// - Span identification (ID, parent ID, operation name)
/// - Timing information (start, end, duration)
/// - Success status and optional error message
/// - Tags dictionary for additional metadata
/// 
/// Used by OrchestrationMetrics for distributed tracing.
/// </summary>
public sealed class TraceSpan
{
    /// <summary>Unique span identifier.</summary>
    public required string SpanId { get; init; }

    /// <summary>Name of the traced operation.</summary>
    public required string OperationName { get; init; }

    /// <summary>Parent span identifier, if nested.</summary>
    public string? ParentSpanId { get; init; }

    /// <summary>UTC start time of the span.</summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>UTC end time of the span, when completed.</summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>Elapsed duration of the span.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Whether the operation completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Key-value tags attached to the span.</summary>
    public Dictionary<string, string> Tags { get; set; } = new();
}
