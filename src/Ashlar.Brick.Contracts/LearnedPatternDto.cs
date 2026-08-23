namespace Ashlar.Brick.Contracts;

/// <summary>
/// A pattern observed during prior brick executions, with confidence and frequency.
/// </summary>
public sealed class LearnedPatternDto
{
    /// <summary>Observed input or output pattern signature.</summary>
    public string Pattern { get; set; } = "";

    /// <summary>Typical outcome when this pattern was observed.</summary>
    public string Outcome { get; set; } = "";

    /// <summary>Model or heuristic confidence in the pattern (0.0–1.0).</summary>
    public double Confidence { get; set; }

    /// <summary>Number of times this pattern has been observed.</summary>
    public int OccurrenceCount { get; set; }
}
