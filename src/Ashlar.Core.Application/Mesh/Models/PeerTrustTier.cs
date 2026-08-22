namespace Ashlar.Core.Application.Mesh.Models;

/// <summary>
/// Trust tier assigned to a discovered mesh peer.
/// </summary>
public enum PeerTrustTier
{
    /// <summary>Trust tier has not been determined.</summary>
    Unknown = 0,

    /// <summary>Peer is explicitly untrusted or blocked.</summary>
    Untrusted = 1,

    /// <summary>Peer is in the trusted peer set.</summary>
    Trusted = 2
}
