namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Information about a discovered peer instance.
/// </summary>
public record PeerInfo
{
    /// <summary>Unique peer identifier.</summary>
    public required string PeerId { get; init; }

    /// <summary>Network or IPC endpoint for reaching this peer.</summary>
    public required string Endpoint { get; init; }

    /// <summary>Capability identifiers advertised by this peer.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();

    /// <summary>Resolved trust tier for routing and admission policy.</summary>
    public PeerTrustTier TrustTier { get; init; } = PeerTrustTier.Unknown;

    /// <summary>When false, peer is excluded from discovery under <c>allowlist</c> admission policy. Defaults to true when omitted in instances.json.</summary>
    public bool Admitted { get; init; } = true;

    /// <summary>When true, peer is excluded from discovery (operator drain).</summary>
    public bool Drained { get; init; }
}
