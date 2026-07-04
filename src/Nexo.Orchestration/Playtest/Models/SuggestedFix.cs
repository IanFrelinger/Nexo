namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// A suggested fix for a balance issue.
/// </summary>
public sealed record SuggestedFix
{
    /// <summary>Human-readable description of the proposed fix.</summary>
    public required string Description { get; init; }

    /// <summary>Confidence score for the suggestion (0–1).</summary>
    public required double Confidence { get; init; }

    /// <summary>Tunable parameters for applying the fix.</summary>
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();
}
