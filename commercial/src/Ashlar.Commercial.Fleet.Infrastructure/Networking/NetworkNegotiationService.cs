using Microsoft.Extensions.Logging;
using Ashlar.Commercial.Fleet.Contracts.Networking.Models;
using Ashlar.Commercial.Fleet.Contracts.Networking.Ports;

namespace Ashlar.Commercial.Fleet.Infrastructure.Networking;
/// <summary>
/// Finds agents on the network by capability or domain for negotiation.
/// </summary>
public sealed class NetworkNegotiationService : INetworkNegotiationService
{
    private readonly INetworkAgentDirectory _directory;
    private readonly ILogger<NetworkNegotiationService> _logger;

    public NetworkNegotiationService(
        INetworkAgentDirectory directory,
        ILogger<NetworkNegotiationService> logger)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetworkAgentEntry>> FindByCapabilityAsync(
        IReadOnlyList<string> capabilities,
        bool refreshFromPeers = true,
        CancellationToken cancellationToken = default)
    {
        if (capabilities == null || capabilities.Count == 0) return Array.Empty<NetworkAgentEntry>();

        var all = await _directory.GetAllAsync(refreshFromPeers, cancellationToken).ConfigureAwait(false);
        var set = new HashSet<string>(capabilities, StringComparer.OrdinalIgnoreCase);
        var list = all.Where(a => a.Capabilities != null && a.Capabilities.Any(c => set.Contains(c))).ToList();
        _logger.LogDebug("Found {Count} agents with capabilities {Capabilities}", list.Count, string.Join(", ", capabilities));
        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetworkAgentEntry>> FindByDomainAsync(
        string domain,
        bool refreshFromPeers = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(domain)) return Array.Empty<NetworkAgentEntry>();

        var all = await _directory.GetAllAsync(refreshFromPeers, cancellationToken).ConfigureAwait(false);
        var list = all.Where(a => string.Equals(a.Domain, domain, StringComparison.OrdinalIgnoreCase)).ToList();
        _logger.LogDebug("Found {Count} agents in domain {Domain}", list.Count, domain);
        return list;
    }
}
