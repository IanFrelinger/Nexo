using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Animation.Models;

/// <summary>
/// Request for animation generation.
/// This record acts as an orchestrator for various animation generation request models and enums,
/// with specific categories defined in partial records/classes.
/// </summary>
public partial record AnimationGenerationRequest
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