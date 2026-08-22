using Ashlar.Commercial.Fleet.Contracts.Networking.Models;

namespace Ashlar.Commercial.Fleet.Contracts.Networking.Ports;
/// <summary>
/// Finds agents on the network that can participate in negotiation (e.g. by conflict type or capability).
/// </summary>
public interface INetworkNegotiationService
{
    /// <summary>Find agents that have any of the given capabilities (e.g. "schema-negotiation", "resource").</summary>
    Task<IReadOnlyList<NetworkAgentEntry>> FindByCapabilityAsync(
        IReadOnlyList<string> capabilities,
        bool refreshFromPeers = true,
        CancellationToken cancellationToken = default);

    /// <summary>Find agents in the given domain (e.g. "Combat", "Economy") across the network.</summary>
    Task<IReadOnlyList<NetworkAgentEntry>> FindByDomainAsync(
        string domain,
        bool refreshFromPeers = true,
        CancellationToken cancellationToken = default);
}
