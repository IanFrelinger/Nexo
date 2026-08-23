namespace Ashlar.Core.Application.Adaptation.Models;

/// <summary>
/// Options for shared adaptation cache (P2.2 mesh sync).
/// </summary>
public sealed class SharedAdaptationOptions
{
    /// <summary>
    /// Peer ID of this instance when broadcasting. Default: ASHLAR_MESH_PEER_ID env or new GUID.
    /// </summary>
    public string? SourcePeerId { get; set; }

    /// <summary>
    /// When non-empty, only adopt adaptations from these peer IDs (or null for local).
    /// When null or empty, adopt from all peers (backward compatible).
    /// </summary>
    public IReadOnlyCollection<string>? TrustedPeerIds { get; set; }
}
