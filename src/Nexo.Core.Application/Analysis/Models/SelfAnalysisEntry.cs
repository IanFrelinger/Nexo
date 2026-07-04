namespace Nexo.Core.Application.Analysis.Models;

/// <summary>Log entry for a Nexo adaptation decision and its outcome.</summary>
public record SelfAnalysisEntry
{
    /// <summary>Stable entry identifier.</summary>
    public required string Id { get; init; }

    /// <summary>UTC timestamp when the decision was recorded.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Type of adaptation decision (e.g. promote, reject, rollback).</summary>
    public required string DecisionType { get; init; }

    /// <summary>Identifier of the adaptation or target entity.</summary>
    public required string TargetId { get; init; }

    /// <summary>Outcome label after the decision was applied.</summary>
    public string? Outcome { get; init; }

    /// <summary>Whether the adaptation improved behavior, when measured.</summary>
    public bool? Improved { get; init; }

    /// <summary>Optional rationale for the decision or outcome.</summary>
    public string? Reason { get; init; }
}
