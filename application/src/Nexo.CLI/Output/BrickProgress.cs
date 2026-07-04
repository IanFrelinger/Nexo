namespace Nexo.CLI.Output;

/// <summary>Progress information for brick execution.</summary>
public class BrickProgress
{
    /// <summary>Executing brick identifier.</summary>
    public required string BrickId { get; init; }

    /// <summary>Human-readable execution status label.</summary>
    public required string Status { get; init; }

    /// <summary>Optional completion percentage (0–100).</summary>
    public int? Progress { get; init; }

    /// <summary>Optional structured metrics emitted during execution.</summary>
    public Dictionary<string, object>? Metrics { get; init; }
}
