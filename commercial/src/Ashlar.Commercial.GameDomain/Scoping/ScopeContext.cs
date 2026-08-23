namespace Ashlar.Commercial.GameDomain.Scoping;

/// <summary>
/// Describes the query point for scope resolution, identifying which player, team, zone,
/// object, and active moment apply at the time of the query.
/// </summary>
public sealed record ScopeContext
{
    /// <summary>Identifier of the player requesting the setting value.</summary>
    public string? PlayerId { get; init; }

    /// <summary>Team the player belongs to, if any.</summary>
    public string? TeamId { get; init; }

    /// <summary>Spatial zone the player is currently located in, if tracked.</summary>
    public string? ZoneId { get; init; }

    /// <summary>Specific object the player is interacting with, if any.</summary>
    public string? ObjectId { get; init; }

    /// <summary>Identifier of the currently active game moment (e.g. <c>"overtime"</c>), if any.</summary>
    public string? ActiveMoment { get; init; }
}
