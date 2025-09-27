using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Animation.Models;

/// <summary>
/// Animation generation results and data structures
/// </summary>
public partial record AnimationGenerationRequest
{
    // Results and data structures are defined here for animation generation
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
