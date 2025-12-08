namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// Represents a playtest session with AI players.
/// </summary>
public sealed record PlaytestSession
{
    public required string SessionId { get; init; }
    public required string BuildId { get; init; }
    public required PlaytestConfiguration Configuration { get; init; }

    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? EndedAt { get; init; }
    public PlaytestStatus Status { get; init; } = PlaytestStatus.Pending;

    public IReadOnlyList<AIPlayerSession> Players { get; init; } = Array.Empty<AIPlayerSession>();
    public PlaytestMetrics? Metrics { get; init; }
}

/// <summary>
/// Configuration for a playtest session.
/// </summary>
public sealed record PlaytestConfiguration
{
    public int PlayerCount { get; init; } = 1;
    public TimeSpan MaxDuration { get; init; } = TimeSpan.FromMinutes(30);
    public string? Scenario { get; init; }
    public IReadOnlyList<string> FocusAreas { get; init; } = Array.Empty<string>();

    /// <summary>
    /// AI player behavior profiles (aggressive, passive, explorer, etc.)
    /// </summary>
    public IReadOnlyList<string> PlayerProfiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Session data for a single AI player.
/// </summary>
public sealed record AIPlayerSession
{
    public required string PlayerId { get; init; }
    public required string Profile { get; init; }
    public IReadOnlyList<PlayerAction> Actions { get; init; } = Array.Empty<PlayerAction>();
    public PlayerMetrics? Metrics { get; init; }
}

/// <summary>
/// An action taken by an AI player.
/// </summary>
public sealed record PlayerAction
{
    public required string ActionType { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();
    public string? Reasoning { get; init; }
}

/// <summary>
/// Metrics for a single player session.
/// </summary>
public sealed record PlayerMetrics
{
    public int TotalActions { get; init; }
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int ObjectivesCompleted { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public TimeSpan TotalPlayTime { get; init; }
}

/// <summary>
/// Overall playtest metrics.
/// </summary>
public sealed record PlaytestMetrics
{
    public TimeSpan TotalDuration { get; init; }
    public int TotalEvents { get; init; }
    public double AverageFrameRate { get; init; }
    public double MinFrameRate { get; init; }
    public long PeakMemoryBytes { get; init; }
    public int CrashCount { get; init; }
    public int ErrorCount { get; init; }
}

/// <summary>
/// Status of a playtest session.
/// </summary>
public enum PlaytestStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

