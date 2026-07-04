namespace Nexo.Commercial.GameDomain.Scoping;

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
