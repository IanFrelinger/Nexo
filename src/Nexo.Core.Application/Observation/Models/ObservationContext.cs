namespace Nexo.Core.Application.Observation.Models;

/// <summary>
/// Assembled context for agent consumption: patterns, recent events, and a summary.
/// </summary>
public sealed record ObservationContext
{
    /// <summary>Detected patterns in the given range.</summary>
    public required IReadOnlyList<ObservedPattern> Patterns { get; init; }

    /// <summary>Recent events (optional; may be empty if not requested).</summary>
    public required IReadOnlyList<NormalizedEvent> RecentEvents { get; init; }

    /// <summary>Human-readable summary for agent consumption.</summary>
    public required string Summary { get; init; }
}
