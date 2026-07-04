namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// Statistical evidence for a balance issue.
/// </summary>
public sealed record StatisticalEvidence
{
    /// <summary>Metric name being compared.</summary>
    public required string Metric { get; init; }

    /// <summary>Value observed during playtesting.</summary>
    public required double ObservedValue { get; init; }

    /// <summary>Expected or baseline value.</summary>
    public required double ExpectedValue { get; init; }

    /// <summary>Deviation from the expected value, if computed.</summary>
    public double? Deviation { get; init; }

    /// <summary>Number of samples used in the comparison.</summary>
    public int SampleSize { get; init; }
}
