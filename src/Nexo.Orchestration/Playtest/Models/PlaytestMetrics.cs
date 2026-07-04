namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// Overall playtest metrics.
/// </summary>
public sealed record PlaytestMetrics
{
    /// <summary>Total elapsed playtest duration.</summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>Total telemetry events recorded.</summary>
    public int TotalEvents { get; init; }

    /// <summary>Mean frame rate during the session.</summary>
    public double AverageFrameRate { get; init; }

    /// <summary>Lowest observed frame rate.</summary>
    public double MinFrameRate { get; init; }

    /// <summary>Peak memory usage in bytes.</summary>
    public long PeakMemoryBytes { get; init; }

    /// <summary>Number of crashes observed.</summary>
    public int CrashCount { get; init; }

    /// <summary>Number of runtime errors observed.</summary>
    public int ErrorCount { get; init; }
}
