using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Animation.Models;

/// <summary>
/// Request for animation generation
/// </summary>
public record AnimationGenerationRequest
{
    /// <summary>
    /// Text prompt describing the animation to generate
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of animation to generate
    /// </summary>
    public AnimationType AnimationType { get; init; } = AnimationType.Walk;
    
    /// <summary>
    /// Duration of the animation in seconds
    /// </summary>
    public float Duration { get; init; } = 2.0f;
    
    /// <summary>
    /// Frame rate for the animation
    /// </summary>
    public int FrameRate { get; init; } = 30;
    
    /// <summary>
    /// Character or object to animate
    /// </summary>
    public string? CharacterType { get; init; }
    
    /// <summary>
    /// Animation style
    /// </summary>
    public string? Style { get; init; }
    
    /// <summary>
    /// Loop the animation
    /// </summary>
    public bool Loop { get; init; } = true;
    
    /// <summary>
    /// Additional parameters specific to the provider
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Result of animation generation
/// </summary>
public record AnimationGenerationResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated animation data
    /// </summary>
    public AnimationData? AnimationData { get; init; }
    
    /// <summary>
    /// File path where animation was saved
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Animation format (FBX, JSON, etc.)
    /// </summary>
    public string Format { get; init; } = "JSON";
    
    /// <summary>
    /// Duration of generated animation
    /// </summary>
    public float Duration { get; init; }
    
    /// <summary>
    /// Frame rate of generated animation
    /// </summary>
    public int FrameRate { get; init; }
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? Error { get; init; }
    
    /// <summary>
    /// Generation time in milliseconds
    /// </summary>
    public long GenerationTimeMs { get; init; }
}

/// <summary>
/// Types of animations that can be generated
/// </summary>
public enum AnimationType
{
    /// <summary>
    /// Walking animation
    /// </summary>
    Walk,
    
    /// <summary>
    /// Running animation
    /// </summary>
    Run,
    
    /// <summary>
    /// Idle animation
    /// </summary>
    Idle,
    
    /// <summary>
    /// Jumping animation
    /// </summary>
    Jump,
    
    /// <summary>
    /// Attack animation
    /// </summary>
    Attack,
    
    /// <summary>
    /// Death animation
    /// </summary>
    Death,
    
    /// <summary>
    /// Custom animation
    /// </summary>
    Custom
}

/// <summary>
/// Animation data structure
/// </summary>
public record AnimationData
{
    /// <summary>
    /// Animation name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Duration in seconds
    /// </summary>
    public float Duration { get; init; }
    
    /// <summary>
    /// Frame rate
    /// </summary>
    public int FrameRate { get; init; }
    
    /// <summary>
    /// Animation tracks for different bones/joints
    /// </summary>
    public Dictionary<string, AnimationTrack> Tracks { get; init; } = new();
    
    /// <summary>
    /// Animation events
    /// </summary>
    public List<AnimationEvent> Events { get; init; } = new();
}

/// <summary>
/// Animation track for a specific bone/joint
/// </summary>
public record AnimationTrack
{
    /// <summary>
    /// Bone/joint name
    /// </summary>
    public string BoneName { get; init; } = string.Empty;
    
    /// <summary>
    /// Position keyframes
    /// </summary>
    public List<Keyframe<Vector3>> PositionKeyframes { get; init; } = new();
    
    /// <summary>
    /// Rotation keyframes
    /// </summary>
    public List<Keyframe<Quaternion>> RotationKeyframes { get; init; } = new();
    
    /// <summary>
    /// Scale keyframes
    /// </summary>
    public List<Keyframe<Vector3>> ScaleKeyframes { get; init; } = new();
}

/// <summary>
/// Animation keyframe
/// </summary>
public record Keyframe<T>
{
    /// <summary>
    /// Time in seconds
    /// </summary>
    public float Time { get; init; }
    
    /// <summary>
    /// Value at this time
    /// </summary>
    public T Value { get; init; } = default!;
    
    /// <summary>
    /// Interpolation type
    /// </summary>
    public InterpolationType Interpolation { get; init; } = InterpolationType.Linear;
}

/// <summary>
/// Animation event
/// </summary>
public record AnimationEvent
{
    /// <summary>
    /// Time when event occurs
    /// </summary>
    public float Time { get; init; }
    
    /// <summary>
    /// Event name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Event parameters
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Interpolation types for keyframes
/// </summary>
public enum InterpolationType
{
    /// <summary>
    /// Linear interpolation
    /// </summary>
    Linear,
    
    /// <summary>
    /// Bezier curve interpolation
    /// </summary>
    Bezier,
    
    /// <summary>
    /// Step interpolation (no interpolation)
    /// </summary>
    Step
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
