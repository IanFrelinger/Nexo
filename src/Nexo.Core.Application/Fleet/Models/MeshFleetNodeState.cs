namespace Nexo.Core.Application.Fleet.Models;

/// <summary>
/// Operator-managed worker registered with the in-process fleet registry.
/// </summary>
public sealed record MeshFleetNodeState(
    string PeerId,
    string ApiBaseUrl,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<string> AdvertisedBrickIds,
    bool Drained,
    DateTimeOffset? LastHeartbeatUtc,
    DateTimeOffset RegisteredAtUtc);
