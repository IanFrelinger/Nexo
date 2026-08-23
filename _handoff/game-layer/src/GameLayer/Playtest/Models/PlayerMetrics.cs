namespace Ashlar.Orchestration.Playtest.Models;

/// <summary>
/// Metrics for a single player session.
/// </summary>
public sealed record PlayerMetrics
{
    /// <summary>Total actions taken by the player.</summary>
    public int TotalActions { get; init; }

    /// <summary>Number of kills recorded.</summary>
    public int Kills { get; init; }

    /// <summary>Number of deaths recorded.</summary>
    public int Deaths { get; init; }

    /// <summary>Objectives completed by the player.</summary>
    public int ObjectivesCompleted { get; init; }

    /// <summary>Mean AI decision latency in milliseconds.</summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>Total active play time for the player.</summary>
    public TimeSpan TotalPlayTime { get; init; }
}
