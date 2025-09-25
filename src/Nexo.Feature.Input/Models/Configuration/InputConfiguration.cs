using System.Collections.Generic;

namespace Nexo.Feature.Input.Models;

/// <summary>
/// Input configuration
/// </summary>
public record InputConfiguration
{
    /// <summary>
    /// Configuration name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Input type
    /// </summary>
    public InputType Type { get; init; }
    
    /// <summary>
    /// Target platform
    /// </summary>
    public string Platform { get; init; } = string.Empty;
    
    /// <summary>
    /// Input actions
    /// </summary>
    public List<InputAction> Actions { get; init; } = new();
    
    /// <summary>
    /// Input bindings
    /// </summary>
    public List<InputBinding> Bindings { get; init; } = new();
    
    /// <summary>
    /// Input maps
    /// </summary>
    public List<InputMap> Maps { get; init; } = new();
    
    /// <summary>
    /// Sensitivity settings
    /// </summary>
    public SensitivitySettings Sensitivity { get; init; } = new();
    
    /// <summary>
    /// Accessibility settings
    /// </summary>
    public AccessibilitySettings Accessibility { get; init; } = new();
}
