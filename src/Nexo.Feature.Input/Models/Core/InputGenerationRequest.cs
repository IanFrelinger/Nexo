using System.Collections.Generic;

namespace Nexo.Feature.Input.Models;

/// <summary>
/// Request for input/controls generation
/// </summary>
public record InputGenerationRequest
{
    /// <summary>
    /// Text prompt describing the input system to generate
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of input system
    /// </summary>
    public InputType InputType { get; init; } = InputType.KeyboardMouse;
    
    /// <summary>
    /// Target platform
    /// </summary>
    public string Platform { get; init; } = "Unity";
    
    /// <summary>
    /// Game genre
    /// </summary>
    public string? GameGenre { get; init; }
    
    /// <summary>
    /// Input sensitivity settings
    /// </summary>
    public SensitivitySettings Sensitivity { get; init; } = new();
    
    /// <summary>
    /// Accessibility options
    /// </summary>
    public AccessibilitySettings Accessibility { get; init; } = new();
    
    /// <summary>
    /// Additional parameters specific to the provider
    /// </summary>
    public Dictionary<string, object> Parameters { get; init; } = new();
}

/// <summary>
/// Result of input system generation
/// </summary>
public record InputGenerationResult
{
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Generated input configuration
    /// </summary>
    public InputConfiguration? Configuration { get; init; }
    
    /// <summary>
    /// File path where configuration was saved
    /// </summary>
    public string? FilePath { get; init; }
    
    /// <summary>
    /// Configuration format (JSON, YAML, etc.)
    /// </summary>
    public string Format { get; init; } = "JSON";
    
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
/// Types of input systems
/// </summary>
public enum InputType
{
    /// <summary>
    /// Keyboard and mouse input
    /// </summary>
    KeyboardMouse,
    
    /// <summary>
    /// Gamepad/controller input
    /// </summary>
    Gamepad,
    
    /// <summary>
    /// Touch input for mobile
    /// </summary>
    Touch,
    
    /// <summary>
    /// VR/AR input
    /// </summary>
    VR,
    
    /// <summary>
    /// Voice input
    /// </summary>
    Voice,
    
    /// <summary>
    /// Gesture input
    /// </summary>
    Gesture,
    
    /// <summary>
    /// Mixed input (multiple types)
    /// </summary>
    Mixed
}
