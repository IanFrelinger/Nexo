using Nexo.Commercial.Fleet.Contracts.Networking.Models;

namespace Nexo.Commercial.Fleet.Contracts.Networking.Ports;
/// <summary>
/// Network-wide event bus for cross-node communication.
/// Extends the local IAgentBus concept across the wire.
/// </summary>
public interface INetworkBus
{
    /// <summary>Broadcast an event to all connected nodes.</summary>
    Task BroadcastAsync(NetworkEvent evt, CancellationToken cancellationToken = default);

    /// <summary>Send an event to a specific node.</summary>
    Task SendAsync(string targetNodeId, NetworkEvent evt, CancellationToken cancellationToken = default);

    /// <summary>Deliver an event received from the wire (dedupe, dispatch to subscribers, optionally relay).</summary>
    Task DeliverAsync(NetworkEvent evt, CancellationToken cancellationToken = default);

    /// <summary>Subscribe to network events by type.</summary>
    Task<IDisposable> SubscribeAsync(
        string? eventType,
        Func<NetworkEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);

    /// <summary>This node's unique identifier.</summary>
    string NodeId { get; }

    /// <summary>Currently known peer nodes.</summary>
    IReadOnlyList<string> ConnectedPeers { get; }
}
