namespace Ashlar.Commercial.GameDomain.Assets;
/// <summary>
/// Input action map descriptor: action maps, actions, bindings, and composite bindings (e.g. FPS controls).
/// </summary>
public sealed record InputDescriptor
{
    /// <summary>id value.</summary>
    public string Id { get; init; } = string.Empty;
    /// <summary>Name value.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Action maps value.</summary>
    public IReadOnlyList<InputActionMap> ActionMaps { get; init; } = Array.Empty<InputActionMap>();
    /// <summary>Tags value.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
