using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Commercial.Fleet.Contracts.Networking.Models;
using Nexo.Commercial.Fleet.Contracts.Networking.Ports;

namespace Nexo.Commercial.Fleet.Infrastructure.Networking;
/// <summary>
/// Aggregates plasticity metrics from optional usage tracker, cache, network bus, knowledge sync, agent directory.
/// </summary>
public sealed class PlasticityService : IPlasticityService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PlasticityOptions _options;
    private readonly ILogger<PlasticityService> _logger;

    public PlasticityService(
        IServiceProvider serviceProvider,
        IOptions<PlasticityOptions> options,
        ILogger<PlasticityService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? new PlasticityOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PlasticityMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var nodeId = "";
        var peerCount = 0;
        var agentCount = 0;
        var hotBrickCount = 0;
        var coldBrickCount = 0;
        double? cacheHitRate = null;
        int? cacheEntries = null;
        long? cacheEvictionCount = null;
        var lastKnowledgeSyncByPeer = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var usageTracker = _serviceProvider.GetService<IBrickUsageTracker>();
        if (usageTracker != null)
        {
            hotBrickCount = usageTracker.GetHotBricks(_options.HotBrickTopN).Count;
            var coldWindow = TimeSpan.FromMinutes(_options.ColdBrickWindowMinutes);
            coldBrickCount = usageTracker.GetColdBricks(coldWindow).Count;
        }

        var cache = _serviceProvider.GetService<IAdaptiveBrickCache>();
        if (cache != null)
        {
            var stats = cache.GetCacheStats();
            cacheHitRate = stats.HitRate;
            cacheEntries = stats.Entries;
            cacheEvictionCount = stats.EvictionCount;
        }

        var networkBus = _serviceProvider.GetService<INetworkBus>();
        if (networkBus != null)
        {
            nodeId = networkBus.NodeId;
            peerCount = networkBus.ConnectedPeers.Count;
        }

        var knowledgeSync = _serviceProvider.GetService<IKnowledgeSyncService>();
        if (knowledgeSync != null)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                var status = await knowledgeSync.GetStatusAsync(cancellationToken).ConfigureAwait(false);
                nodeId = status.NodeId;
            }
            var status2 = await knowledgeSync.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            foreach (var kv in status2.LastSyncByPeer)
                lastKnowledgeSyncByPeer[kv.Key] = kv.Value.ToString("O");
        }

        var agentDirectory = _serviceProvider.GetService<INetworkAgentDirectory>();
        if (agentDirectory != null)
        {
            if (string.IsNullOrEmpty(nodeId)) nodeId = agentDirectory.NodeId;
            var agents = await agentDirectory.GetAllAsync(refreshFromPeers: false, cancellationToken).ConfigureAwait(false);
            agentCount = agents.Count;
        }

        return new PlasticityMetrics
        {
            NodeId = nodeId,
            PeerCount = peerCount,
            AgentCount = agentCount,
            HotBrickCount = hotBrickCount,
            ColdBrickCount = coldBrickCount,
            CacheHitRate = cacheHitRate,
            CacheEntries = cacheEntries,
            CacheEvictionCount = cacheEvictionCount,
            LastKnowledgeSyncByPeer = lastKnowledgeSyncByPeer
        };
    }
}
