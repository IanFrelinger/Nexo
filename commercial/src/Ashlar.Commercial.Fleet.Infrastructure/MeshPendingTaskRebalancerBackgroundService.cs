using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;

namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>
/// Periodically calls <see cref="IMeshTaskPlacementService.TryScheduleAsync"/> for pending tasks that have been waiting longer than <see cref="MeshElasticSchedulingOptions.PendingStaleSeconds"/>.
/// </summary>
public sealed class MeshPendingTaskRebalancerBackgroundService : BackgroundService
{
    private readonly IMeshTaskRegistry _tasks;
    private readonly IMeshTaskPlacementService _placement;
    private readonly IOptionsMonitor<MeshElasticSchedulingOptions> _options;
    private readonly ILogger<MeshPendingTaskRebalancerBackgroundService>? _logger;

    public MeshPendingTaskRebalancerBackgroundService(
        IMeshTaskRegistry tasks,
        IMeshTaskPlacementService placement,
        IOptionsMonitor<MeshElasticSchedulingOptions> options,
        ILogger<MeshPendingTaskRebalancerBackgroundService>? logger = null)
    {
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _placement = placement ?? throw new ArgumentNullException(nameof(placement));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = _options.CurrentValue;
            var interval = TimeSpan.FromMinutes(Math.Max(1, opts.IntervalMinutes));
            var stale = TimeSpan.FromSeconds(Math.Max(30, opts.PendingStaleSeconds));

            try
            {
                if (opts.Enabled)
                {
                    var now = DateTimeOffset.UtcNow;
                    var list = await _tasks.ListAsync(stoppingToken).ConfigureAwait(false);
                    foreach (var t in list)
                    {
                        if (t.Status != MeshTaskStatus.Pending)
                            continue;
                        if (now - t.CreatedAtUtc < stale)
                            continue;

                        var (ok, _, err) = await _placement.TryScheduleAsync(t.TaskId, cancellationToken: stoppingToken).ConfigureAwait(false);
                        if (ok)
                        {
                            _logger?.LogInformation(
                                "mesh-elastic rebalanced task={TaskId} (was pending since {Created})",
                                t.TaskId,
                                t.CreatedAtUtc);
                        }
                        else if (err is not null && err != "placement.no_eligible_nodes")
                        {
                            _logger?.LogDebug("mesh-elastic skip task={TaskId} reason={Reason}", t.TaskId, err);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "mesh-elastic round failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
