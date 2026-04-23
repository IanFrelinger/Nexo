namespace Nexo.Core.Application.Mesh.Models;

/// <summary>
/// Information about a discovered peer instance.
/// </summary>
public record PeerInfo
{
    public required string PeerId { get; init; }
    public required string Endpoint { get; init; }
    public IReadOnlyList<string> Capabilities { get; init; } = Array.Empty<string>();
    public PeerTrustTier TrustTier { get; init; } = PeerTrustTier.Unknown;

    /// <summary>When false, peer is excluded from discovery under <c>allowlist</c> admission policy. Defaults to true when omitted in instances.json.</summary>
    public bool Admitted { get; init; } = true;

    /// <summary>When true, peer is excluded from discovery (operator drain).</summary>
    public bool Drained { get; init; }
}

public enum PeerTrustTier
{
    Unknown = 0,
    Untrusted = 1,
    Trusted = 2
}
