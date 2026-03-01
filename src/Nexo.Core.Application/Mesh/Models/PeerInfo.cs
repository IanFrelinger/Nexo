namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Information about a discovered peer instance.
/// </summary>
public record PeerInfo
{
    public required string PeerId { get; init; }
    public required string Endpoint { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();
}
