namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Input action record.</summary>
public sealed record InputAction
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty; // Move, Look, Fire, Reload, Sprint, Crouch, Jump, Interact, ADS
    /// <summary>Type value.</summary>
    public string Type { get; init; } = "button"; // button, value, passthrough
    /// <summary>Control type value.</summary>
    public string ControlType { get; init; } = "Button"; // Button, Vector2, Axis, etc.
    /// <summary>Bindings value.</summary>
    public IReadOnlyList<InputBinding> Bindings { get; init; } = Array.Empty<InputBinding>();
}
