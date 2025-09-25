using System.Collections.Generic;

namespace Nexo.Feature.Input.Models;

/// <summary>
/// Input binding definition
/// </summary>
public record InputBinding
{
    /// <summary>
    /// Action name this binding is for
    /// </summary>
    public string ActionName { get; init; } = string.Empty;
    
    /// <summary>
    /// Input device
    /// </summary>
    public InputDevice Device { get; init; }
    
    /// <summary>
    /// Input key/button
    /// </summary>
    public string Key { get; init; } = string.Empty;
    
    /// <summary>
    /// Binding type
    /// </summary>
    public InputBindingType Type { get; init; }
    
    /// <summary>
    /// Modifier keys
    /// </summary>
    public List<string> Modifiers { get; init; } = new();
    
    /// <summary>
    /// Binding priority
    /// </summary>
    public int Priority { get; init; } = 0;
}

/// <summary>
/// Input devices
/// </summary>
public enum InputDevice
{
    /// <summary>
    /// Keyboard
    /// </summary>
    Keyboard,
    
    /// <summary>
    /// Mouse
    /// </summary>
    Mouse,
    
    /// <summary>
    /// Gamepad
    /// </summary>
    Gamepad,
    
    /// <summary>
    /// Touch screen
    /// </summary>
    Touch,
    
    /// <summary>
    /// VR controller
    /// </summary>
    VRController,
    
    /// <summary>
    /// Voice input
    /// </summary>
    Voice,
    
    /// <summary>
    /// Custom device
    /// </summary>
    Custom
}

/// <summary>
/// Input binding types
/// </summary>
public enum InputBindingType
{
    /// <summary>
    /// Press binding
    /// </summary>
    Press,
    
    /// <summary>
    /// Hold binding
    /// </summary>
    Hold,
    
    /// <summary>
    /// Release binding
    /// </summary>
    Release,
    
    /// <summary>
    /// Tap binding
    /// </summary>
    Tap,
    
    /// <summary>
    /// Double tap binding
    /// </summary>
    DoubleTap,
    
    /// <summary>
    /// Long press binding
    /// </summary>
    LongPress,
    
    /// <summary>
    /// Swipe binding
    /// </summary>
    Swipe,
    
    /// <summary>
    /// Pinch binding
    /// </summary>
    Pinch
}
