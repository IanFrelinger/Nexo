using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Domain.Execution.Events;

/// <summary>
/// Base class for all execution events.
/// </summary>
public abstract class ExecutionEvent
{
    /// <summary>Event type identifier (e.g. behavior_started, step_completed).</summary>
    public string Type { get; init; } = default!;
    /// <summary>When the event occurred (UTC).</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>Creates a base execution event.</summary>
    /// <param name="type">Canonical event type identifier.</param>
    /// <param name="timestamp">UTC timestamp when the event occurred.</param>
    protected ExecutionEvent(string type, DateTime timestamp)
    {
        Type = type;
        Timestamp = timestamp;
    }
}
