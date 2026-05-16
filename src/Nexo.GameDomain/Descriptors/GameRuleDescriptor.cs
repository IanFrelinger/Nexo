namespace Nexo.GameDomain.Descriptors;

/// <summary>
/// Data-only descriptor for a game rule set that governs match flow, scoring, and respawn behaviour.
/// <para>
/// A single <see cref="GameRuleDescriptor"/> is active per session and is consumed by the
/// host game-mode logic to enforce win conditions, round timers, and team structure.
/// </para>
/// </summary>
public sealed record GameRuleDescriptor
{
    /// <summary>Stable identifier used to reference this rule set across sessions.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name shown in mode selection UI.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Game mode that determines match structure and win conditions.
    /// Accepted values: <c>"deathmatch"</c>, <c>"team_deathmatch"</c>, <c>"capture"</c>,
    /// <c>"king_of_hill"</c>, <c>"custom"</c>.
    /// </summary>
    public string Mode { get; init; } = "deathmatch";

    /// <summary>Maximum duration of a single round in seconds.</summary>
    public int RoundDurationSeconds { get; init; } = 300;

    /// <summary>Score target that triggers match completion.</summary>
    public int MaxScore { get; init; } = 25;

    /// <summary>Number of teams (1 for FFA modes, 2+ for team modes).</summary>
    public int TeamCount { get; init; } = 1;

    /// <summary>Delay in seconds before a killed player respawns.</summary>
    public double RespawnDelaySeconds { get; init; } = 3.0;

    /// <summary>Whether team members can damage each other.</summary>
    public bool FriendlyFire { get; init; }

    /// <summary>
    /// Arbitrary key/value pairs for mode-specific or user-defined rules
    /// (e.g. <c>"overtime_enabled"</c>, <c>"flag_return_time"</c>).
    /// </summary>
    public IReadOnlyDictionary<string, object> CustomProperties { get; init; } =
        new Dictionary<string, object>();
}
