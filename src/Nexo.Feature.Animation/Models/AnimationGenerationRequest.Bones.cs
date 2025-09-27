using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Animation.Models;

/// <summary>
/// Bone and constraint data structures
/// </summary>
public partial record AnimationGenerationRequest
{
    // Bone and constraint data structures are defined here
}

/// <summary>
/// Bone definition
/// </summary>
public record Bone
{
    /// <summary>
    /// Bone name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Parent bone name
    /// </summary>
    public string? ParentName { get; init; }
    
    /// <summary>
    /// Bone position
    /// </summary>
    public Vector3 Position { get; init; }
    
    /// <summary>
    /// Bone rotation
    /// </summary>
    public Quaternion Rotation { get; init; }
    
    /// <summary>
    /// Bone scale
    /// </summary>
    public Vector3 Scale { get; init; } = Vector3.One;
}

/// <summary>
/// Constraint definition
/// </summary>
public record Constraint
{
    /// <summary>
    /// Constraint name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Constraint type
    /// </summary>
    public ConstraintType Type { get; init; }
    
    /// <summary>
    /// Source bone
    /// </summary>
    public string SourceBone { get; init; } = string.Empty;
    
    /// <summary>
    /// Target bone
    /// </summary>
    public string TargetBone { get; init; } = string.Empty;
    
    /// <summary>
    /// Constraint parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Constraint types
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// Position constraint
    /// </summary>
    Position,
    
    /// <summary>
    /// Rotation constraint
    /// </summary>
    Rotation,
    
    /// <summary>
    /// Scale constraint
    /// </summary>
    Scale,
    
    /// <summary>
    /// Look-at constraint
    /// </summary>
    LookAt,
    
    /// <summary>
    /// IK constraint
    /// </summary>
    IK
}
