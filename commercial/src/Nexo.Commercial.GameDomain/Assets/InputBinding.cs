namespace Nexo.Commercial.GameDomain.Assets;

/// <summary>Input binding record.</summary>
public sealed record InputBinding
{
    /// <summary>Path value.</summary>
    public string Path { get; init; } = string.Empty; // e.g., "<Keyboard>/w", "<Mouse>/leftButton", "<Gamepad>/leftStick"
    /// <summary>Composite value.</summary>
    public string? Composite { get; init; } // For composite bindings: "2DVector", "1DAxis", etc.
    /// <summary>Composite part value.</summary>
    public string? CompositePart { get; init; } // "up", "down", "left", "right"
    /// <summary>Interactions value.</summary>
    public IReadOnlyList<string> Interactions { get; init; } = Array.Empty<string>(); // "hold", "tap", "slowTap", "multiTap"
    /// <summary>Processors value.</summary>
    public IReadOnlyList<string> Processors { get; init; } = Array.Empty<string>(); // "normalize", "invert", "scale(factor=2)"
}
