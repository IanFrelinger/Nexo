namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Descriptor for a capability that can be advertised.
/// </summary>
public record CapabilityDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> CanProduce { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Needs { get; init; } = Array.Empty<string>();
}
