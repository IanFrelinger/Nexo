using Nexo.Core.Application.Networking.Models;

namespace Nexo.Core.Application.Networking.Ports;

/// <summary>
/// Directory of agents on the network: register local agents and discover agents on peers.
/// </summary>
public interface INetworkAgentDirectory
{
    /// <summary>This node's unique identifier.</summary>
    string NodeId { get; }

    /// <summary>Register or update a local agent entry.</summary>
    Task RegisterAsync(NetworkAgentEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Unregister a local agent by id.</summary>
    Task UnregisterAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>Get all agents known for a given node (local or discovered).</summary>
    Task<IReadOnlyList<NetworkAgentEntry>> GetByNodeAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>Get an agent by id (local or discovered).</summary>
    Task<NetworkAgentEntry?> GetByAgentIdAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>Get all known agents (local + discovered). Refresh from peers if stale.</summary>
    Task<IReadOnlyList<NetworkAgentEntry>> GetAllAsync(bool refreshFromPeers = false, CancellationToken cancellationToken = default);

    /// <summary>Refresh directory by querying peer nodes for their agents.</summary>
    Task RefreshFromPeersAsync(CancellationToken cancellationToken = default);
}
