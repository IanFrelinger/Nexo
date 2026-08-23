namespace Ashlar.Core.Application.Adaptation.Models;

/// <summary>
/// Record of an adaptation attempt: what was fixed, regression result, promoted or not.
/// </summary>
public record AdaptationRecord
{
    /// <summary>Unique identifier for this adaptation attempt.</summary>
    public required string Id { get; init; }

    /// <summary>UTC timestamp when the adaptation was attempted.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Brick identifier when the fix targeted a brick.</summary>
    public string? BrickId { get; init; }

    /// <summary>Failure type that triggered the adaptation.</summary>
    public required string FailureType { get; init; }

    /// <summary>Whether a source file edit or brick manifest recompile was applied.</summary>
    public required AdaptationFixType FixApplied { get; init; }

    /// <summary>Source file path when <see cref="FixApplied"/> is <see cref="AdaptationFixType.Source"/>.</summary>
    public string? FilePath { get; init; }

    /// <summary>Whether regression tests passed after the fix.</summary>
    public bool RegressionPassed { get; init; }

    /// <summary>Whether the fix was promoted to the active core.</summary>
    public bool Promoted { get; init; }

    /// <summary>Human-readable outcome or error message.</summary>
    public string? Message { get; init; }
}
