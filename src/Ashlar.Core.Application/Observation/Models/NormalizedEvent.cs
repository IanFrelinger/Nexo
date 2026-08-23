using System.Text.Json;

namespace Ashlar.Core.Application.Observation.Models;

/// <summary>
/// Common schema for all observed events regardless of source.
/// Used by the observation pipeline for normalization, gating, and pattern detection.
/// </summary>
public sealed record NormalizedEvent
{
    /// <summary>Unique identifier for this event.</summary>
    public required string EventId { get; init; }

    /// <summary>When the event occurred.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Source identifier (e.g. file-system, process-monitor, build).</summary>
    public required string SourceId { get; init; }

    /// <summary>Data category for Trust gating (e.g. file-paths, process-names).</summary>
    public required string Category { get; init; }

    /// <summary>Optional project path for per-project gating.</summary>
    public string? ProjectPath { get; init; }

    /// <summary>Event-type specific payload (path, process name, etc.).</summary>
    public JsonElement? Payload { get; init; }
}
