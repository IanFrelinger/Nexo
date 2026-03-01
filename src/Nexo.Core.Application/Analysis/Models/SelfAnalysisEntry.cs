namespace Nexo.Core.Application.Analysis.Models;

/// <summary>
/// Log entry for a Nexo adaptation decision and its outcome.
/// </summary>
public record SelfAnalysisEntry
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required string DecisionType { get; init; }
    public required string TargetId { get; init; }
    public string? Outcome { get; init; }
    public bool? Improved { get; init; }
    public string? Reason { get; init; }
}
