using Ashlar.Core.Domain.Bricks;

namespace Ashlar.Core.Application.Adaptation.Models;

/// <summary>
/// Editable representation of a brick. Produced by decomposer, consumed by recompiler.
/// </summary>
public record BrickManifest
{
    /// <summary>Unique brick identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable brick name.</summary>
    public required string Name { get; init; }

    /// <summary>Semantic version of the brick definition.</summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>Optional description of brick purpose.</summary>
    public string? Description { get; init; }

    /// <summary>Brick category for catalog and routing.</summary>
    public BrickCategory Category { get; init; }

    /// <summary>Input and output port definitions.</summary>
    public required BrickInterface Interface { get; init; }

    /// <summary>Assembly-qualified type name for existing bricks, or null for generated.</summary>
    public string? ImplementationTypeName { get; init; }

    /// <summary>Generated implementation source (C#) when creating new bricks.</summary>
    public string? ImplementationSource { get; init; }

    /// <summary>Provenance marker for generated source (fixture, model provider, etc.).</summary>
    public string? GenerationProvenance { get; init; }

    /// <summary>Declared class name when source is generated.</summary>
    public string? GeneratedClassName { get; init; }

    /// <summary>Declared namespace when source is generated.</summary>
    public string? GeneratedNamespace { get; init; }

    /// <summary>Configuration overrides for the implementation.</summary>
    public IReadOnlyDictionary<string, object> Config { get; init; } = new Dictionary<string, object>();
}
