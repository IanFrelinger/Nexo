namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// A telemetry event from game execution.
/// 
/// Represents a single event captured during playtesting:
/// - Event identification and type
/// - Session and player information
/// - Timestamp and event data
/// 
/// Used by playtest agents to record gameplay events for analysis.
/// </summary>
public sealed record TelemetryEvent
{
    /// <summary>Unique event identifier.</summary>
    public required string EventId { get; init; }

    /// <summary>Playtest session that produced the event.</summary>
    public required string SessionId { get; init; }

    /// <summary>Event type label.</summary>
    public required string EventType { get; init; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Player associated with the event, if any.</summary>
    public string? PlayerId { get; init; }

    /// <summary>Event-specific payload data.</summary>
    public IReadOnlyDictionary<string, object> Data { get; init; } =
        new Dictionary<string, object>();
}
