using Ashlar.Core.Application.Adaptation.Models;
using Ashlar.Core.Application.Observation.Models;

namespace Ashlar.Core.Application.SelfContext.Models;

/// <summary>Assembled self-context: recent adaptations, executions, patterns, and summary for agents.</summary>
public record SelfContextModel
{
    /// <summary>Recent adaptation records for agent context.</summary>
    public required IReadOnlyList<AdaptationRecord> RecentAdaptations { get; init; }

    /// <summary>Recent execution trace entries.</summary>
    public required IReadOnlyList<ExecutionTraceEntry> RecentExecutions { get; init; }

    /// <summary>Recently observed behavioral patterns.</summary>
    public required IReadOnlyList<ObservedPattern> RecentPatterns { get; init; }

    /// <summary>Human-readable summary of current self-state.</summary>
    public required string Summary { get; init; }
}
