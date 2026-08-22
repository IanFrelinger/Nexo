namespace Ashlar.Spatial.Runtime.Ports;

/// <summary>
/// Spatial-facing certified atom identity (no cert-layer geo/manufacture fields).
/// </summary>
public sealed record ResolvedAtomIdentity(
    string AtomId,
    string AssetHash,
    string AssetVersion);
