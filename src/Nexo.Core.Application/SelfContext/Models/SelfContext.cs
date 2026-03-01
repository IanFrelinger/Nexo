using Nexo.Core.Application.Adaptation.Models;
using Nexo.Core.Application.Observation.Models;

namespace Nexo.Core.Application.SelfContext.Models;

/// <summary>
/// Assembled self-context: recent adaptations, executions, patterns, and summary for agents.
/// </summary>
public record SelfContextModel
{
    public required IReadOnlyList<AdaptationRecord> RecentAdaptations { get; init; }
    public required IReadOnlyList<ExecutionTraceEntry> RecentExecutions { get; init; }
    public required IReadOnlyList<ObservedPattern> RecentPatterns { get; init; }
    public required string Summary { get; init; }
}
