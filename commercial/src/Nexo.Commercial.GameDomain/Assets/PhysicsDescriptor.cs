namespace Nexo.GameDomain.Assets;

/// <summary>
/// Physics configuration descriptor. Covers physics materials (friction/bounce),
/// collider setups, rigidbody configurations, and projectile physics profiles.
/// </summary>
public sealed record PhysicsDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = "material"; // material, collider, rigidbody, projectile

    // Physics material properties
    public double DynamicFriction { get; init; } = 0.6;
    public double StaticFriction { get; init; } = 0.6;
    public double Bounciness { get; init; } = 0.0;
    public string FrictionCombine { get; init; } = "average"; // average, minimum, maximum, multiply
    public string BounceCombine { get; init; } = "average";

    // Rigidbody properties
    public double Mass { get; init; } = 1.0;
    public double Drag { get; init; } = 0.0;
    public double AngularDrag { get; init; } = 0.05;
    public bool UseGravity { get; init; } = true;
    public bool IsKinematic { get; init; }
    public string Interpolation { get; init; } = "none"; // none, interpolate, extrapolate
    public string CollisionDetection { get; init; } = "discrete"; // discrete, continuous, continuous_dynamic, continuous_speculative

    // Collider configuration
    public string ColliderType { get; init; } = "box"; // box, sphere, capsule, mesh, convex_mesh
    public Vector3Descriptor ColliderCenter { get; init; } = new();
    public Vector3Descriptor ColliderSize { get; init; } = new(1, 1, 1);
    public double ColliderRadius { get; init; } = 0.5;
    public double ColliderHeight { get; init; } = 2.0;
    public bool IsTrigger { get; init; }

    // Projectile physics profile
    public double ProjectileSpeed { get; init; } = 50.0;
    public double GravityMultiplier { get; init; } = 1.0;
    public double MaxLifetime { get; init; } = 5.0;
    public int MaxBounces { get; init; } = 0;
    public double BounceEnergyRetention { get; init; } = 0.6;

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
