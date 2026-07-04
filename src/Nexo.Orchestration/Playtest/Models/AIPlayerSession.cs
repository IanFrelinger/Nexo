namespace Nexo.Orchestration.Playtest.Models;

/// <summary>
/// Session data for a single AI player.
/// </summary>
public sealed record AIPlayerSession
{
    /// <summary>Unique player identifier within the session.</summary>
    public required string PlayerId { get; init; }

    /// <summary>Behavior profile assigned to the player.</summary>
    public required string Profile { get; init; }

    /// <summary>Actions taken by the player during the session.</summary>
    public IReadOnlyList<PlayerAction> Actions { get; init; } = Array.Empty<PlayerAction>();

    /// <summary>Per-player metrics for the session.</summary>
    public PlayerMetrics? Metrics { get; init; }
}
