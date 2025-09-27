using System.Numerics;

namespace Nexo.Feature.Physics.Models;

/// <summary>
/// Bounds and utility structures
/// </summary>
public partial class PhysicsGenerationRequest
{
    // Bounds models are defined in separate files
}

/// <summary>
/// Bounds structure
/// </summary>
public record Bounds
{
    /// <summary>
    /// Center point
    /// </summary>
    public Vector3 Center { get; init; }
    
    /// <summary>
    /// Size/extents
    /// </summary>
    public Vector3 Size { get; init; }
    
    /// <summary>
    /// Minimum corner
    /// </summary>
    public Vector3 Min => Center - Size * 0.5f;
    
    /// <summary>
    /// Maximum corner
    /// </summary>
    public Vector3 Max => Center + Size * 0.5f;
}
