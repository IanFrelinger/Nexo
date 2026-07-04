namespace Nexo.Commercial.GameDomain.Playtest;

/// <summary>Performance metrics record.</summary>
public sealed record PerformanceMetrics
{
    /// <summary>Average fps value.</summary>
    public double AverageFps { get; init; }
    /// <summary>P99 fps value.</summary>
    public double P99Fps { get; init; }
    /// <summary>Draw calls value.</summary>
    public int DrawCalls { get; init; }
    /// <summary>Memory mb value.</summary>
    public double MemoryMb { get; init; }
    /// <summary>Active particles value.</summary>
    public int ActiveParticles { get; init; }
}
