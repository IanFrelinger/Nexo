namespace Nexo.Agents.UniversalTester.Models;

/// <summary>
/// A generic action that can be executed on any target.
/// </summary>
public record TestAction
{
    public required string Id { get; init; }
    public required ActionType Type { get; init; }
    public required string Description { get; init; }
    
    // Target specification (one of these based on type)
    public string? Selector { get; init; }      // CSS selector, XPath, UI Automation ID
    public Vector2? Coordinates { get; init; }  // Screen coordinates
    public string? ElementId { get; init; }     // Game object ID, element ID
    
    // Action parameters
    public string? InputValue { get; init; }    // Text to type, value to set
    public string? Key { get; init; }           // Keyboard key
    public MouseButton? MouseButton { get; init; }
    public TimeSpan? Duration { get; init; }    // For hold actions
    public Vector2? DragTarget { get; init; }   // For drag operations
    
    // API-specific
    public string? HttpMethod { get; init; }
    public string? Endpoint { get; init; }
    public string? RequestBody { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    
    // CLI-specific
    public string? Command { get; init; }
    
    // Expectations
    public string? ExpectedOutcome { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}

public enum ActionType
{
    // Universal
    Wait,
    Screenshot,
    
    // Mouse
    Click,
    DoubleClick,
    RightClick,
    Hover,
    Drag,
    Scroll,
    
    // Keyboard
    Type,
    KeyPress,
    KeyDown,
    KeyUp,
    Shortcut,
    
    // Navigation
    Navigate,
    Back,
    Forward,
    Refresh,
    
    // Form
    Fill,
    Select,
    Check,
    Uncheck,
    
    // Game-specific
    Move,           // WASD/joystick movement
    Look,           // Mouse look / camera
    Jump,
    Attack,
    Interact,
    UseItem,
    OpenMenu,
    
    // API
    ApiRequest,
    
    // CLI
    ExecuteCommand,
    SendInput,
    
    // System
    LaunchApp,
    CloseApp,
    SwitchWindow,
    Resize
}

public enum MouseButton
{
    Left,
    Right,
    Middle
}
