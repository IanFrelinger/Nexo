using Ashlar.Core.Domain;

namespace Ashlar.Commercial.Fleet.Infrastructure.Networking;
/// <summary>
/// Configuration for the HTTP-based network bus.
/// </summary>
public class NetworkBusOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "NetworkBus";

    /// <summary>This node's unique identifier. Default: machine name.</summary>
    public string NodeId { get; set; } = Environment.MachineName;

    /// <summary>Base URLs of peer nodes (e.g. https://ashlar-a.example.com).</summary>
    public IReadOnlyList<string> PeerUrls { get; set; } = [];

    /// <summary>Heartbeat interval in seconds. Default 30.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = AshlarDefaults.NetworkBusHeartbeatIntervalSeconds;

    /// <summary>Maximum event IDs to keep for deduplication. Default 10,000.</summary>
    public int MaxEventHistory { get; set; } = AshlarDefaults.NetworkBusMaxEventHistory;

    /// <summary>Default max hops for propagation. Default 3.</summary>
    public int DefaultMaxHops { get; set; } = AshlarDefaults.NetworkBusDefaultMaxHops;
}
