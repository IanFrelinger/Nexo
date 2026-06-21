using Nexo.Core.Domain.Bricks;

namespace Nexo.Core.Application.Adaptation.Models;

/// <summary>
/// Editable representation of a brick. Produced by decomposer, consumed by recompiler.
/// </summary>
public record BrickManifest
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Version { get; init; } = "1.0.0";
    public string? Description { get; init; }
    public BrickCategory Category { get; init; }
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
