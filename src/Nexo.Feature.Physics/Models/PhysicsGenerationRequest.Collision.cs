using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Physics.Models;

/// <summary>
/// Collision shapes and materials
/// </summary>
public partial class PhysicsGenerationRequest
{
    // Collision models are defined in separate files
}

/// <summary>
/// Collision shape
/// </summary>
public record CollisionShape
{
    /// <summary>
    /// Shape type
    /// </summary>
    public ShapeType Type { get; init; } = ShapeType.Box;
    
    /// <summary>
    /// Shape dimensions
    /// </summary>
    public Vector3 Dimensions { get; init; } = Vector3.One;
    
    /// <summary>
    /// Shape radius (for spheres and capsules)
    /// </summary>
    public float Radius { get; init; } = 0.5f;
    
    /// <summary>
    /// Shape height (for capsules)
    /// </summary>
    public float Height { get; init; } = 1.0f;
}

/// <summary>
/// Physics material
/// </summary>
public record PhysicsMaterial
{
    /// <summary>
    /// Material name
    /// </summary>
    public string Name { get; init; } = "Default";
    
    /// <summary>
    /// Friction coefficient
    /// </summary>
    public float Friction { get; init; } = 0.5f;
    
    /// <summary>
    /// Restitution (bounciness)
    /// </summary>
    public float Restitution { get; init; } = 0.0f;
    
    /// <summary>
    /// Density
    /// </summary>
    public float Density { get; init; } = 1.0f;
}
