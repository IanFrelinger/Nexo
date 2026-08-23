namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Patrol route record.</summary>
public sealed record PatrolRoute
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Waypoints value.</summary>
    public IReadOnlyList<Vector3Descriptor> Waypoints { get; init; } = Array.Empty<Vector3Descriptor>();
    /// <summary>Loop value.</summary>
    public bool Loop { get; init; } = true;
    /// <summary>Wait time per point value.</summary>
    public double WaitTimePerPoint { get; init; } = 2.0;
    /// <summary>Move speed value.</summary>
    public double MoveSpeed { get; init; } = 3.0;
}
