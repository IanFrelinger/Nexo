namespace Ashlar.Core.Application.Composition.Models;

/// <summary>
/// Descriptor for a capability component. Maps to bricks (PerceptionBrick, ValidationBrick, etc.).
/// </summary>
public record ComponentDescriptor
{
    /// <summary>Stable component identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Capability name this component implements.</summary>
    public required string Capability { get; init; }

    /// <summary>Human-readable component name for UIs.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Short description of component behavior.</summary>
    public required string Summary { get; init; }

    /// <summary>JSON schema for component inputs, when defined.</summary>
    public string? InputSchema { get; init; }

    /// <summary>JSON schema for component outputs, when defined.</summary>
    public string? OutputSchema { get; init; }

    /// <summary>CLR type name of the component implementation.</summary>
    public required string ImplementationType { get; init; }

    /// <summary>Semantic version of the component.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Support level indicating stability of the component.</summary>
    public ComponentSupportLevel SupportLevel { get; init; } = ComponentSupportLevel.Stable;

    /// <summary>Capabilities required before this component can be used.</summary>
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = Array.Empty<string>();

    /// <summary>Component identifiers that cannot coexist with this one.</summary>
    public IReadOnlyList<string> IncompatibleWith { get; init; } = Array.Empty<string>();
}
