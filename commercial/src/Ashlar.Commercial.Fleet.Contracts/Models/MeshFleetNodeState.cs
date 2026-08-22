namespace Ashlar.Commercial.Fleet.Contracts.Models;

/// <summary>
/// Operator-managed worker registered with the in-process fleet registry.
/// <see cref="ReportedQueueDepth"/> is the last-reported local queue depth from heartbeat (0 = unknown / idle).
/// </summary>
public sealed record MeshFleetNodeState(
    string PeerId,
    string ApiBaseUrl,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<string> AdvertisedBrickIds,
    bool Drained,
    DateTimeOffset? LastHeartbeatUtc,
    DateTimeOffset RegisteredAtUtc,
    int ReportedQueueDepth = 0,
    MeshFleetTrustTier TrustTier = MeshFleetTrustTier.Trusted,
    bool Admitted = true,
    string? RegistrationKeyFingerprint = null);
