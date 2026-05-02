namespace Nexo.GameDomain.Assets;

/// <summary>
/// Input action map descriptor for Unity Input System. Defines action maps,
/// actions, bindings, and composite bindings for FPS controls.
/// </summary>
public sealed record InputDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<InputActionMap> ActionMaps { get; init; } = Array.Empty<InputActionMap>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

public sealed record InputActionMap
{
    public string Name { get; init; } = string.Empty; // Gameplay, UI, Vehicle, Spectator
    public IReadOnlyList<InputAction> Actions { get; init; } = Array.Empty<InputAction>();
}

public sealed record InputAction
{
    public string Name { get; init; } = string.Empty; // Move, Look, Fire, Reload, Sprint, Crouch, Jump, Interact, ADS
    public string Type { get; init; } = "button"; // button, value, passthrough
    public string ControlType { get; init; } = "Button"; // Button, Vector2, Axis, etc.
    public IReadOnlyList<InputBinding> Bindings { get; init; } = Array.Empty<InputBinding>();
}

public sealed record InputBinding
{
    public string Path { get; init; } = string.Empty; // e.g., "<Keyboard>/w", "<Mouse>/leftButton", "<Gamepad>/leftStick"
    public string? Composite { get; init; } // For composite bindings: "2DVector", "1DAxis", etc.
    public string? CompositePart { get; init; } // "up", "down", "left", "right"
    public IReadOnlyList<string> Interactions { get; init; } = Array.Empty<string>(); // "hold", "tap", "slowTap", "multiTap"
    public IReadOnlyList<string> Processors { get; init; } = Array.Empty<string>(); // "normalize", "invert", "scale(factor=2)"
}
