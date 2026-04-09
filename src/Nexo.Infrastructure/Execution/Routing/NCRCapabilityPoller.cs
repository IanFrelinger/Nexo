using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Execution.Routing;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Infrastructure.NodeCapabilityRuntime;

namespace Nexo.Infrastructure.Execution.Routing;

/// <summary>
/// Background poller that keeps NCR capability snapshot current for routing.
/// </summary>
public sealed class NCRCapabilityPoller : BackgroundService, INCRCapabilitySnapshot
{
    private readonly IHardwareProfiler _hardwareProfiler;
    private readonly ILocalQueueDepthProvider _queueDepthProvider;
    private readonly IOptions<NodeCapabilityRuntimeOptions> _runtimeOptions;
    private readonly ILogger<NCRCapabilityPoller> _logger;
    private volatile SnapshotState _snapshot = new();

    public NCRCapabilityPoller(
        IHardwareProfiler hardwareProfiler,
        ILocalQueueDepthProvider queueDepthProvider,
        IOptions<NodeCapabilityRuntimeOptions> runtimeOptions,
        ILogger<NCRCapabilityPoller> logger)
    {
        _hardwareProfiler = hardwareProfiler ?? throw new ArgumentNullException(nameof(hardwareProfiler));
        _queueDepthProvider = queueDepthProvider ?? throw new ArgumentNullException(nameof(queueDepthProvider));
        _runtimeOptions = runtimeOptions ?? throw new ArgumentNullException(nameof(runtimeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public long AvailableVramBytes => _snapshot.AvailableVramBytes;

    public GpuComputeClass ComputeClass => _snapshot.ComputeClass;

    public int CurrentQueueDepth => _snapshot.CurrentQueueDepth;

    public DateTimeOffset CapturedAt => _snapshot.CapturedAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshSnapshotAsync(stoppingToken).ConfigureAwait(false);
        var cadence = _runtimeOptions.Value.ProfileRefreshInterval > TimeSpan.Zero
            ? _runtimeOptions.Value.ProfileRefreshInterval
            : TimeSpan.FromSeconds(30);

        using var timer = new PeriodicTimer(cadence);
        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await RefreshSnapshotAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshSnapshotAsync(CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _hardwareProfiler.CaptureAsync(cancellationToken).ConfigureAwait(false);
            var computeClass = ResolveComputeClass(profile.Tier);
            var queueDepth = Math.Max(0, _queueDepthProvider.CurrentDepth);
            _snapshot = new SnapshotState
            {
                AvailableVramBytes = profile.AvailableVRAMBytes,
                ComputeClass = computeClass,
                CurrentQueueDepth = queueDepth,
                CapturedAt = DateTimeOffset.UtcNow
            };

            _logger.LogDebug(
                "ncr-capability-poller refreshed snapshot vram={VramBytes} compute={ComputeClass} queueDepth={QueueDepth}",
                _snapshot.AvailableVramBytes,
                _snapshot.ComputeClass,
                _snapshot.CurrentQueueDepth);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful cancellation path.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ncr-capability-poller failed to refresh snapshot");
        }
    }

    private static GpuComputeClass ResolveComputeClass(Nexo.Core.Application.NodeCapabilityRuntime.Models.NodeTier tier)
    {
        var envOverride = Environment.GetEnvironmentVariable("NEXO_GPU_COMPUTE_CLASS");
        if (!string.IsNullOrWhiteSpace(envOverride) &&
            Enum.TryParse<GpuComputeClass>(envOverride, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return tier switch
        {
            Nexo.Core.Application.NodeCapabilityRuntime.Models.NodeTier.Core => GpuComputeClass.Extreme,
            Nexo.Core.Application.NodeCapabilityRuntime.Models.NodeTier.Standard => GpuComputeClass.High,
            Nexo.Core.Application.NodeCapabilityRuntime.Models.NodeTier.Micro => GpuComputeClass.Medium,
            Nexo.Core.Application.NodeCapabilityRuntime.Models.NodeTier.Nano => GpuComputeClass.Low,
            _ => GpuComputeClass.None
        };
    }

    private sealed record SnapshotState
    {
        public long AvailableVramBytes { get; init; }
        public GpuComputeClass ComputeClass { get; init; } = GpuComputeClass.None;
        public int CurrentQueueDepth { get; init; }
        public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
