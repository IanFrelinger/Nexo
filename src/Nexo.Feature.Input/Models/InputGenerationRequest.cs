using System.Collections.Generic;
using System.Numerics;

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

/// <summary>
/// Input action definition
/// </summary>
public record InputAction
{
    /// <summary>
    /// Action name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Action description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Action type
    /// </summary>
    public InputActionType Type { get; init; }
    
    /// <summary>
    /// Action value type
    /// </summary>
    public InputValueType ValueType { get; init; }
    
    /// <summary>
    /// Whether action is required
    /// </summary>
    public bool Required { get; init; } = true;
    
    /// <summary>
    /// Default value
    /// </summary>
    public object? DefaultValue { get; init; }
}

/// <summary>
/// Input action types
/// </summary>
public enum InputActionType
{
    /// <summary>
    /// Button press
    /// </summary>
    Button,
    
    /// <summary>
    /// Axis movement
    /// </summary>
    Axis,
    
    /// <summary>
    /// Vector2 movement
    /// </summary>
    Vector2,
    
    /// <summary>
    /// Vector3 movement
    /// </summary>
    Vector3,
    
    /// <summary>
    /// Quaternion rotation
    /// </summary>
    Quaternion,
    
    /// <summary>
    /// Custom action
    /// </summary>
    Custom
}

/// <summary>
/// Input value types
/// </summary>
public enum InputValueType
{
    /// <summary>
    /// Boolean value
    /// </summary>
    Boolean,
    
    /// <summary>
    /// Float value
    /// </summary>
    Float,
    
    /// <summary>
    /// Integer value
    /// </summary>
    Integer,
    
    /// <summary>
    /// Vector2 value
    /// </summary>
    Vector2,
    
    /// <summary>
    /// Vector3 value
    /// </summary>
    Vector3,
    
    /// <summary>
    /// Quaternion value
    /// </summary>
    Quaternion,
    
    /// <summary>
    /// String value
    /// </summary>
    String
}

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

/// <summary>
/// Input map definition
/// </summary>
public record InputMap
{
    /// <summary>
    /// Map name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Map description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Map priority
    /// </summary>
    public int Priority { get; init; } = 0;
    
    /// <summary>
    /// Actions in this map
    /// </summary>
    public List<string> Actions { get; init; } = new();
    
    /// <summary>
    /// Whether map is active
    /// </summary>
    public bool IsActive { get; init; } = true;
    
    /// <summary>
    /// Map conditions
    /// </summary>
    public List<InputCondition> Conditions { get; init; } = new();
}

/// <summary>
/// Input condition
/// </summary>
public record InputCondition
{
    /// <summary>
    /// Condition type
    /// </summary>
    public InputConditionType Type { get; init; }
    
    /// <summary>
    /// Condition value
    /// </summary>
    public object? Value { get; init; }
    
    /// <summary>
    /// Condition operator
    /// </summary>
    public InputConditionOperator Operator { get; init; }
}

/// <summary>
/// Input condition types
/// </summary>
public enum InputConditionType
{
    /// <summary>
    /// Game state condition
    /// </summary>
    GameState,
    
    /// <summary>
    /// Player state condition
    /// </summary>
    PlayerState,
    
    /// <summary>
    /// Input device condition
    /// </summary>
    InputDevice,
    
    /// <summary>
    /// Custom condition
    /// </summary>
    Custom
}

/// <summary>
/// Input condition operators
/// </summary>
public enum InputConditionOperator
{
    /// <summary>
    /// Equals
    /// </summary>
    Equals,
    
    /// <summary>
    /// Not equals
    /// </summary>
    NotEquals,
    
    /// <summary>
    /// Greater than
    /// </summary>
    GreaterThan,
    
    /// <summary>
    /// Less than
    /// </summary>
    LessThan,
    
    /// <summary>
    /// Contains
    /// </summary>
    Contains,
    
    /// <summary>
    /// Not contains
    /// </summary>
    NotContains
}

