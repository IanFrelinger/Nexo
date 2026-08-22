namespace Ashlar.Commercial.GameDomain.Scoping;
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
