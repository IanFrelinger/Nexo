namespace Nexo.GameDomain.Scoping;

/// <summary>
/// A single setting value bound to a specific scope within the game world hierarchy.
/// <para>
/// Scoped settings enable per-team, per-zone, or per-player overrides of global values.
/// The <see cref="ScopeResolver"/> evaluates these at query time using a narrowest-wins
/// precedence model.
/// </para>
/// </summary>
public sealed record ScopedSetting
{
    /// <summary>Identifier of the setting being overridden (e.g. <c>"gravity"</c>, <c>"time_scale"</c>).</summary>
    public string SettingId { get; init; } = string.Empty;

    /// <summary>The override value for this setting within the declared scope.</summary>
    public object Value { get; init; } = null!;

    /// <summary>The scope to which this setting applies.</summary>
    public SettingScope Scope { get; init; } = new();

    /// <summary>Identifier of the user or system that created this override.</summary>
    public string CreatedBy { get; init; } = string.Empty;

    /// <summary>Timestamp when this override was created.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Optional expiration. When set, the override is automatically ignored after this instant.
    /// Useful for time-limited event modifiers.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Identifies the scope tier and optional target entity for a <see cref="ScopedSetting"/>.
/// </summary>
public sealed record SettingScope
{
    /// <summary>Tier in the scope hierarchy (broadest to narrowest).</summary>
    public SettingScopeType Type { get; init; } = SettingScopeType.Server;

    /// <summary>
    /// Optional target within the tier — e.g. a team name, zone id, player id, or object id.
    /// <c>null</c> for <see cref="SettingScopeType.Server"/> scope (applies globally).
    /// </summary>
    public string? Target { get; init; }
}

/// <summary>
/// Hierarchical scope tiers ordered from broadest to narrowest.
/// The <see cref="ScopeResolver"/> walks from <see cref="Moment"/> (narrowest)
/// to <see cref="Server"/> (broadest), returning the first match.
/// </summary>
public enum SettingScopeType
{
    /// <summary>Applies to the entire server / session.</summary>
    Server = 0,

    /// <summary>Applies to a specific team.</summary>
    Team = 1,

    /// <summary>Applies to a spatial zone within the map.</summary>
    Zone = 2,

    /// <summary>Applies to a specific interactive object.</summary>
    Object = 3,

    /// <summary>Applies to an individual player.</summary>
    Player = 4,

    /// <summary>
    /// Applies only during a transient game moment (e.g. overtime, power-play).
    /// Narrowest scope — always wins when active.
    /// </summary>
    Moment = 5
}
