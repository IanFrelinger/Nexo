namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Physics configuration descriptor. Covers physics materials (friction/bounce),
/// collider setups, rigidbody configurations, and projectile physics profiles.
/// </summary>
public sealed record PhysicsDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Category value.</summary>
    public string Category { get; init; } = "material"; // material, collider, rigidbody, projectile

    // Physics material properties
    /// <summary>Dynamic friction value.</summary>
    public double DynamicFriction { get; init; } = 0.6;
    /// <summary>Static friction value.</summary>
    public double StaticFriction { get; init; } = 0.6;
    /// <summary>Bounciness value.</summary>
    public double Bounciness { get; init; } = 0.0;
    /// <summary>Friction combine value.</summary>
    public string FrictionCombine { get; init; } = "average"; // average, minimum, maximum, multiply
    /// <summary>Bounce combine value.</summary>
    public string BounceCombine { get; init; } = "average";

    // Rigidbody properties
    /// <summary>Mass value.</summary>
    public double Mass { get; init; } = 1.0;
    /// <summary>Drag value.</summary>
    public double Drag { get; init; } = 0.0;
    /// <summary>Angular drag value.</summary>
    public double AngularDrag { get; init; } = 0.05;
    /// <summary>Use gravity value.</summary>
    public bool UseGravity { get; init; } = true;
    /// <summary>Whether kinematic.</summary>
    public bool IsKinematic { get; init; }
    /// <summary>Interpolation value.</summary>
    public string Interpolation { get; init; } = "none"; // none, interpolate, extrapolate
    /// <summary>Collision detection value.</summary>
    public string CollisionDetection { get; init; } = "discrete"; // discrete, continuous, continuous_dynamic, continuous_speculative

    // Collider configuration
    /// <summary>Collider type value.</summary>
    public string ColliderType { get; init; } = "box"; // box, sphere, capsule, mesh, convex_mesh
    /// <summary>Collider center value.</summary>
    public Vector3Descriptor ColliderCenter { get; init; } = new();
    /// <summary>Collider size value.</summary>
    public Vector3Descriptor ColliderSize { get; init; } = new(1, 1, 1);
    /// <summary>Collider radius value.</summary>
    public double ColliderRadius { get; init; } = 0.5;
    /// <summary>Collider height value.</summary>
    public double ColliderHeight { get; init; } = 2.0;
    /// <summary>Whether trigger.</summary>
    public bool IsTrigger { get; init; }

    // Projectile physics profile
    /// <summary>Projectile speed value.</summary>
    public double ProjectileSpeed { get; init; } = 50.0;
    /// <summary>Gravity multiplier value.</summary>
    public double GravityMultiplier { get; init; } = 1.0;
    /// <summary>Max lifetime value.</summary>
    public double MaxLifetime { get; init; } = 5.0;
    /// <summary>Max bounces value.</summary>
    public int MaxBounces { get; init; } = 0;
    /// <summary>Bounce energy retention value.</summary>
    public double BounceEnergyRetention { get; init; } = 0.6;

    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
