namespace Nexo.Core.Domain.Bricks;

/// <summary>
/// A pattern learned from production usage that helps improve accuracy.
/// </summary>
public class LearnedPattern
{
    /// <summary>Observed input or output pattern signature.</summary>
    public string Pattern { get; init; } = default!;

    /// <summary>Typical outcome when this pattern was observed.</summary>
    public string Outcome { get; init; } = default!;

    /// <summary>Model or heuristic confidence in the pattern (0.0–1.0).</summary>
    public double Confidence { get; init; }

    /// <summary>Number of times this pattern has been observed.</summary>
    public int OccurrenceCount { get; init; }

    /// <summary>Records a learned execution pattern.</summary>
    /// <param name="pattern">Observed pattern signature.</param>
    /// <param name="outcome">Typical outcome for the pattern.</param>
    /// <param name="confidence">Confidence score between 0.0 and 1.0.</param>
    /// <param name="occurrenceCount">Number of observed occurrences.</param>
    public LearnedPattern(string pattern, string outcome, double confidence, int occurrenceCount)
    {
        Pattern = pattern;
        Outcome = outcome;
        Confidence = confidence;
        OccurrenceCount = occurrenceCount;
    }
}
