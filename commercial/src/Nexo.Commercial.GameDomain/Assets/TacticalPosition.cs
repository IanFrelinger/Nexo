namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Tactical position record.</summary>
public sealed record TacticalPosition
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Position value.</summary>
    public Vector3Descriptor Position { get; init; } = new();
    /// <summary>Role value.</summary>
    public string Role { get; init; } = "generic"; // sniper_nest, choke_point, flank_route, power_position, objective_watch
    /// <summary>Control radius value.</summary>
    public double ControlRadius { get; init; } = 10.0;
    /// <summary>Sightline targets value.</summary>
    public IReadOnlyList<Vector3Descriptor> SightlineTargets { get; init; } = Array.Empty<Vector3Descriptor>();
}
