namespace Ashlar.Commercial.GameDomain.Scoping;

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
