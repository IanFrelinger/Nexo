using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Abstractions.Agents;

namespace Nexo.Orchestration.Metrics;

/// <summary>
/// Report for a trace span.
/// </summary>
public sealed record SpanReport
{
    /// <summary>Unique span identifier.</summary>
    public required string SpanId { get; init; }

    /// <summary>Name of the traced operation.</summary>
    public required string OperationName { get; init; }

    /// <summary>Parent span identifier, if nested.</summary>
    public string? ParentSpanId { get; init; }

    /// <summary>Elapsed duration of the span.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Whether the operation completed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>Key-value tags attached to the span.</summary>
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}
