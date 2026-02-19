namespace Nexo.Agents.UniversalTester.Models;

/// <summary>
/// A generic action that can be executed on any target.
/// </summary>
public record TestAction
{
    /// <summary>Unique action identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Type of action (click, type, ApiRequest, etc.).</summary>
    public required ActionType Type { get; init; }
    /// <summary>Human-readable description.</summary>
    public required string Description { get; init; }

    /// <summary>CSS selector, XPath, or UI Automation ID.</summary>
    public string? Selector { get; init; }
    /// <summary>Screen coordinates (x, y) for click/type.</summary>
    public Vector2? Coordinates { get; init; }
    /// <summary>Game object ID or element ID.</summary>
    public string? ElementId { get; init; }

    /// <summary>Text to type or value to set.</summary>
    public string? InputValue { get; init; }
    /// <summary>Keyboard key (Enter, Tab, etc.).</summary>
    public string? Key { get; init; }
    /// <summary>Mouse button for click actions.</summary>
    public MouseButton? MouseButton { get; init; }
    /// <summary>Duration for hold/wait actions.</summary>
    public TimeSpan? Duration { get; init; }
    /// <summary>Target position for drag operations.</summary>
    public Vector2? DragTarget { get; init; }

    /// <summary>HTTP method for API requests.</summary>
    public string? HttpMethod { get; init; }
    /// <summary>API endpoint path.</summary>
    public string? Endpoint { get; init; }
    /// <summary>JSON request body for API.</summary>
    public string? RequestBody { get; init; }
    /// <summary>HTTP headers for API request.</summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>Command to execute (CLI targets).</summary>
    public string? Command { get; init; }

    /// <summary>Expected outcome for validation.</summary>
    public string? ExpectedOutcome { get; init; }
    /// <summary>Timeout for the action.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Types of test actions that can be executed on any target.
/// </summary>
public enum ActionType
{
    /// <summary>Wait for a duration.</summary>
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

/// <summary>
/// Mouse button for click actions.
/// </summary>
public enum MouseButton
{
    /// <summary>Left mouse button.</summary>
    Left,
    /// <summary>Right mouse button.</summary>
    Right,
    /// <summary>Middle mouse button.</summary>
    Middle
}
