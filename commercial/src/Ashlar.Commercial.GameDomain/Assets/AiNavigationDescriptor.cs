namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// AI navigation descriptor for defining patrol routes, cover points,
/// tactical positions, sightlines, and navigation areas for AI bots.
/// </summary>
public sealed record AiNavigationDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Patrol routes value.</summary>
    public IReadOnlyList<PatrolRoute> PatrolRoutes { get; init; } = Array.Empty<PatrolRoute>();
    /// <summary>Cover points value.</summary>
    public IReadOnlyList<CoverPoint> CoverPoints { get; init; } = Array.Empty<CoverPoint>();
    /// <summary>Tactical positions value.</summary>
    public IReadOnlyList<TacticalPosition> TacticalPositions { get; init; } = Array.Empty<TacticalPosition>();
    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
