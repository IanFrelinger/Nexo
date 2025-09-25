namespace Nexo.Feature.Input.Models;

/// <summary>
/// Input sensitivity settings
/// </summary>
public record SensitivitySettings
{
    /// <summary>
    /// Mouse sensitivity
    /// </summary>
    public float MouseSensitivity { get; init; } = 1.0f;
    
    /// <summary>
    /// Gamepad sensitivity
    /// </summary>
    public float GamepadSensitivity { get; init; } = 1.0f;
    
    /// <summary>
    /// Touch sensitivity
    /// </summary>
    public float TouchSensitivity { get; init; } = 1.0f;
    
    /// <summary>
    /// Dead zone for analog sticks
    /// </summary>
    public float DeadZone { get; init; } = 0.1f;
    
    /// <summary>
    /// Invert Y axis
    /// </summary>
    public bool InvertY { get; init; } = false;
    
    /// <summary>
    /// Invert X axis
    /// </summary>
    public bool InvertX { get; init; } = false;
}
