namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// Represents a playtest session with AI players.
/// 
/// Contains:
/// - Session identification and configuration
/// - Start/end times and status
/// - AI player sessions and actions
/// - Playtest metrics
/// 
/// Used by playtest agents to track and analyze playtest sessions.
/// </summary>
public sealed record PlaytestSession
{
    /// <summary>Unique session identifier.</summary>
    public required string SessionId { get; init; }

    /// <summary>Build under test.</summary>
    public required string BuildId { get; init; }

    /// <summary>Session configuration and parameters.</summary>
    public required PlaytestConfiguration Configuration { get; init; }

    /// <summary>UTC timestamp when the session started.</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>UTC timestamp when the session ended.</summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>Current lifecycle status of the session.</summary>
    public PlaytestStatus Status { get; init; } = PlaytestStatus.Pending;

    /// <summary>AI player sessions participating in the playtest.</summary>
    public IReadOnlyList<AIPlayerSession> Players { get; init; } = Array.Empty<AIPlayerSession>();

    /// <summary>Aggregate metrics collected during the session.</summary>
    public PlaytestMetrics? Metrics { get; init; }
}
