namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Descriptor for a capability that can be advertised.
/// </summary>
public record CapabilityDescriptor
{
    /// <summary>Stable capability identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable capability name.</summary>
    public required string Name { get; init; }

    /// <summary>Artifact or data types this capability can produce.</summary>
    public IReadOnlyList<string> CanProduce { get; init; } = Array.Empty<string>();

    /// <summary>Artifact or data types this capability requires as input.</summary>
    public IReadOnlyList<string> Needs { get; init; } = Array.Empty<string>();
}
