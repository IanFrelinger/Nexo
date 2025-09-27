using System.Collections.Generic;
using System.Numerics;

namespace Nexo.Feature.Animation.Models;

/// <summary>
/// Animation types and enums
/// </summary>
public partial record AnimationGenerationRequest
{
    // Types and enums are defined here for animation generation
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
