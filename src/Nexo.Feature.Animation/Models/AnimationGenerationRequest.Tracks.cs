using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Animation.Models;

/// <summary>
/// Animation tracks and keyframes
/// </summary>
public partial record AnimationGenerationRequest
{
    // Animation tracks and keyframes are defined here
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
