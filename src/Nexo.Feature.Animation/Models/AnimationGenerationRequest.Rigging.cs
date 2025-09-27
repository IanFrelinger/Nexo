using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Animation.Models;

/// <summary>
/// Rigging request and data structures
/// </summary>
public partial record AnimationGenerationRequest
{
    // Rigging request and data structures are defined here
}

/// <summary>
/// Rigging request
/// </summary>
public record RiggingRequest
{
    /// <summary>
    /// Text prompt describing the rig to generate
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of rig to generate
    /// </summary>
    public RigType RigType { get; init; } = RigType.Humanoid;
    
    /// <summary>
    /// Character type
    /// </summary>
    public string? CharacterType { get; init; }
    
    /// <summary>
    /// Additional parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Rigging result
/// </summary>
public record RiggingResult
{
    /// <summary>
    /// Whether rigging was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated rig data
    /// </summary>
    public RigData? RigData { get; init; }
    
    /// <summary>
    /// File path where rig was saved
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Error message if rigging failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Generation time in milliseconds
    /// </summary>
    public long GenerationTimeMs { get; init; }
}

/// <summary>
/// Types of rigs
/// </summary>
public enum RigType
{
    /// <summary>
    /// Humanoid rig
    /// </summary>
    Humanoid,
    
    /// <summary>
    /// Quadruped rig
    /// </summary>
    Quadruped,
    
    /// <summary>
    /// Custom rig
    /// </summary>
    Custom
}

/// <summary>
/// Rig data structure
/// </summary>
public record RigData
{
    /// <summary>
    /// Rig name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Rig type
    /// </summary>
    public RigType Type { get; init; }
    
    /// <summary>
    /// Bones in the rig
    /// </summary>
    public List<Bone> Bones { get; init; } = new();
    
    /// <summary>
    /// Constraints in the rig
    /// </summary>
    public List<Constraint> Constraints { get; init; } = new();
}
