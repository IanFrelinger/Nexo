namespace Ashlar.Core.Application.Common.Ports;

/// <summary>Snapshot of collected metrics.</summary>
public record MetricsSnapshot
{
    /// <summary>Recorded execution durations keyed by operation name.</summary>
    public required IReadOnlyDictionary<string, TimeSpan> ExecutionTimes { get; init; }

    /// <summary>Monotonic counters keyed by metric name.</summary>
    public required IReadOnlyDictionary<string, long> Counters { get; init; }

    /// <summary>UTC timestamp when this snapshot was collected.</summary>
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
}
