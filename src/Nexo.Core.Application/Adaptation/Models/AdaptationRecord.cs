namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Record of an adaptation attempt: what was fixed, regression result, promoted or not.
/// </summary>
public record AdaptationRecord
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? BrickId { get; init; }
    public required string FailureType { get; init; }
    /// <summary>Source = file edit; Brick = manifest recompile.</summary>
    public required AdaptationFixType FixApplied { get; init; }
    public string? FilePath { get; init; }
    public bool RegressionPassed { get; init; }
    public bool Promoted { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// Type of fix applied.
/// </summary>
public enum AdaptationFixType
{
    Source,
    Brick,
}
