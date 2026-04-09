using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.Mesh.Ports;

namespace Nexo.Infrastructure.Execution.Routing;

/// <summary>
/// Lightweight poller that keeps peer capability snapshots current for routing.
/// </summary>
public sealed class PeerCapabilitySnapshotPoller : BackgroundService, IPeerCapabilitySnapshot
{
    private readonly IInstanceDiscovery _discovery;
    private readonly ILogger<PeerCapabilitySnapshotPoller> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly object _gate = new();
    private IReadOnlyList<PeerExecutionCandidate> _candidates = Array.Empty<PeerExecutionCandidate>();
    private readonly string _capabilityId;

    public PeerCapabilitySnapshotPoller(
        IInstanceDiscovery discovery,
        ILogger<PeerCapabilitySnapshotPoller> logger,
        IOptions<RunPodBrickConfig> config)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var configured = config?.Value?.PeerDiscoveryInterval ?? TimeSpan.FromSeconds(10);
        _pollInterval = configured <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : configured;
        _capabilityId = string.IsNullOrWhiteSpace(config?.Value?.PeerCapabilityId)
            ? "generation.capability-routing"
            : config!.Value.PeerCapabilityId;
    }

    public IReadOnlyList<PeerExecutionCandidate> Candidates
    {
        get
        {
            lock (_gate)
            {
                return _candidates;
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var discovered = await _discovery.DiscoverAsync(stoppingToken).ConfigureAwait(false);
                var next = discovered.Select(MapPeer).ToArray();
                lock (_gate)
                {
                    _candidates = next;
                }

                _logger.LogDebug("peer-capabilities snapshot updated peers={PeerCount}", next.Length);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "peer-capabilities snapshot refresh failed");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private PeerExecutionCandidate MapPeer(Nexo.Core.Application.Mesh.Models.PeerInfo peer)
    {
        var vram = ParseLongCapability(peer.Capabilities, "vram");
        var queueDepth = ParseIntCapability(peer.Capabilities, "queue");
        var compute = ParseComputeClass(peer.Capabilities);
        var supportsGeneration = peer.Capabilities.Any(cap =>
            cap.Contains(_capabilityId, StringComparison.OrdinalIgnoreCase) ||
            cap.Contains("generation", StringComparison.OrdinalIgnoreCase) ||
            cap.Contains("nexo-cli", StringComparison.OrdinalIgnoreCase));
        if (!supportsGeneration)
        {
            return new PeerExecutionCandidate
            {
                PeerId = peer.PeerId,
                Endpoint = string.Empty
            };
        }

        return new PeerExecutionCandidate
        {
            PeerId = peer.PeerId,
            Endpoint = peer.Endpoint,
            AvailableVramBytes = vram,
            ComputeClass = compute,
            QueueDepth = queueDepth,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    private static long ParseLongCapability(IEnumerable<string> capabilities, string key)
    {
        foreach (var capability in capabilities)
        {
            if (capability.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
            {
                var value = capability[(key.Length + 1)..];
                if (long.TryParse(value, out var parsed))
                {
                    return parsed;
                }
            }
        }

        return 0;
    }

    private static int ParseIntCapability(IEnumerable<string> capabilities, string key)
    {
        foreach (var capability in capabilities)
        {
            if (capability.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
            {
                var value = capability[(key.Length + 1)..];
                if (int.TryParse(value, out var parsed))
                {
                    return parsed;
                }
            }
        }

        return 0;
    }

    private static GpuComputeClass ParseComputeClass(IEnumerable<string> capabilities)
    {
        foreach (var capability in capabilities)
        {
            if (capability.StartsWith("compute:", StringComparison.OrdinalIgnoreCase))
            {
                var text = capability["compute:".Length..];
                if (Enum.TryParse<GpuComputeClass>(text, true, out var parsed))
                {
                    return parsed;
                }
            }
        }

        return GpuComputeClass.None;
    }
}
