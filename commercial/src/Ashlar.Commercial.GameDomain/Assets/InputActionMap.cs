namespace Ashlar.Commercial.GameDomain.Assets;

/// <summary>Input action map record.</summary>
public sealed record InputActionMap
{
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty; // Gameplay, UI, Vehicle, Spectator
    /// <summary>Actions value.</summary>
    public IReadOnlyList<InputAction> Actions { get; init; } = Array.Empty<InputAction>();
}
