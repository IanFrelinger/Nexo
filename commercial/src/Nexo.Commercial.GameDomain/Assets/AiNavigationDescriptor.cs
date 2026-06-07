namespace Nexo.GameDomain.Assets;

/// <summary>
/// AI navigation descriptor for defining patrol routes, cover points,
/// tactical positions, sightlines, and navigation areas for AI bots.
/// </summary>
public sealed record AiNavigationDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<PatrolRoute> PatrolRoutes { get; init; } = Array.Empty<PatrolRoute>();
    public IReadOnlyList<CoverPoint> CoverPoints { get; init; } = Array.Empty<CoverPoint>();
    public IReadOnlyList<TacticalPosition> TacticalPositions { get; init; } = Array.Empty<TacticalPosition>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record PatrolRoute
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<Vector3Descriptor> Waypoints { get; init; } = Array.Empty<Vector3Descriptor>();
    public bool Loop { get; init; } = true;
    public double WaitTimePerPoint { get; init; } = 2.0;
    public double MoveSpeed { get; init; } = 3.0;
}

public sealed record CoverPoint
{
    public Vector3Descriptor Position { get; init; } = new();
    public Vector3Descriptor CoverDirection { get; init; } = new(); // Direction the cover faces (where threat comes from)
    public string CoverType { get; init; } = "half"; // half, full, lean_left, lean_right
    public double Rating { get; init; } = 1.0; // 0-1 quality rating for AI selection
}

public sealed record TacticalPosition
{
    public string Name { get; init; } = string.Empty;
    public Vector3Descriptor Position { get; init; } = new();
    public string Role { get; init; } = "generic"; // sniper_nest, choke_point, flank_route, power_position, objective_watch
    public double ControlRadius { get; init; } = 10.0;
    public IReadOnlyList<Vector3Descriptor> SightlineTargets { get; init; } = Array.Empty<Vector3Descriptor>();
}
