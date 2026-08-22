using LiteDB;
using Ashlar.Commercial.Fleet.Contracts.Models;

namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>Mesh fleet node doc.</summary>
internal sealed class MeshFleetNodeDoc
{
    /// <summary>LiteDB primary key and fleet peer identifier.</summary>
    [BsonId]
    public string PeerId { get; set; } = "";

    /// <summary>Api base url.</summary>
    public string ApiBaseUrl { get; set; } = "";
    /// <summary>Labels.</summary>
    public Dictionary<string, string> Labels { get; set; } = new();
    /// <summary>Advertised brick ids.</summary>
    public List<string> AdvertisedBrickIds { get; set; } = new();
    /// <summary>Drained.</summary>
    public bool Drained { get; set; }
    /// <summary>Last heartbeat utc.</summary>
    public DateTimeOffset? LastHeartbeatUtc { get; set; }
    /// <summary>Registered at utc.</summary>
    public DateTimeOffset RegisteredAtUtc { get; set; }
    /// <summary>Reported queue depth.</summary>
    public int ReportedQueueDepth { get; set; }
    /// <summary>Trust tier.</summary>
    public int TrustTier { get; set; }
    /// <summary>Admitted.</summary>
    public bool Admitted { get; set; } = true;
    /// <summary>Registration key fingerprint.</summary>
    public string? RegistrationKeyFingerprint { get; set; }

    /// <summary>From state.</summary>
    /// <param name="n">N.</param>
    public static MeshFleetNodeDoc FromState(MeshFleetNodeState n) => new()
    {
        PeerId = n.PeerId,
        ApiBaseUrl = n.ApiBaseUrl,
        Labels = n.Labels.ToDictionary(static kv => kv.Key, static kv => kv.Value),
        AdvertisedBrickIds = n.AdvertisedBrickIds.ToList(),
        Drained = n.Drained,
        LastHeartbeatUtc = n.LastHeartbeatUtc,
        RegisteredAtUtc = n.RegisteredAtUtc,
        ReportedQueueDepth = n.ReportedQueueDepth,
        TrustTier = (int)n.TrustTier,
        Admitted = n.Admitted,
        RegistrationKeyFingerprint = n.RegistrationKeyFingerprint,
    };

    /// <summary>To state.</summary>
    public MeshFleetNodeState ToState() => new(
        PeerId,
        ApiBaseUrl,
        Labels,
        AdvertisedBrickIds,
        Drained,
        LastHeartbeatUtc,
        RegisteredAtUtc,
        ReportedQueueDepth,
        (MeshFleetTrustTier)TrustTier,
        Admitted,
        RegistrationKeyFingerprint);
}
