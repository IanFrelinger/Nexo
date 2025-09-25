namespace Nexo.Feature.Input.Models;

/// <summary>
/// Accessibility settings
/// </summary>
public record AccessibilitySettings
{
    /// <summary>
    /// Enable colorblind support
    /// </summary>
    public bool EnableColorblindSupport { get; init; } = false;
    
    /// <summary>
    /// Enable high contrast mode
    /// </summary>
    public bool EnableHighContrast { get; init; } = false;
    
    /// <summary>
    /// Enable large text mode
    /// </summary>
    public bool EnableLargeText { get; init; } = false;
    
    /// <summary>
    /// Enable one-handed mode
    /// </summary>
    public bool EnableOneHandedMode { get; init; } = false;
    
    /// <summary>
    /// Enable voice commands
    /// </summary>
    public bool EnableVoiceCommands { get; init; } = false;
    
    /// <summary>
    /// Enable haptic feedback
    /// </summary>
    public bool EnableHapticFeedback { get; init; } = true;
}
