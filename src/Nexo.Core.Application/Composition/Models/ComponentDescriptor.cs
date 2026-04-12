namespace Nexo.Core.Application.Composition.Models;

/// <summary>
/// Descriptor for a capability component. Maps to bricks (PerceptionBrick, ValidationBrick, etc.).
/// </summary>
public record ComponentDescriptor
{
    public required string Id { get; init; }
    public required string Capability { get; init; }
    public required string DisplayName { get; init; }
    public required string Summary { get; init; }
    public string? InputSchema { get; init; }
    public string? OutputSchema { get; init; }
    public required string ImplementationType { get; init; }
    public string Version { get; init; } = "1.0.0";
    public ComponentSupportLevel SupportLevel { get; init; } = ComponentSupportLevel.Stable;
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> IncompatibleWith { get; init; } = Array.Empty<string>();
}

public enum ComponentSupportLevel
{
    Stable = 0,
    Experimental = 1,
    Placeholder = 2,
}
