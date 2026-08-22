using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;

namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>
/// Clears expired execution leases by moving <see cref="MeshTaskStatus.Assigned"/> or <see cref="MeshTaskStatus.Running"/> tasks back to <see cref="MeshTaskStatus.Pending"/>.
/// </summary>
public sealed class MeshLeaseSweepBackgroundService : BackgroundService
{
    private readonly IMeshTaskRegistry _tasks;
    private readonly IOptionsMonitor<MeshCheckpointOptions> _options;
    private readonly ILogger<MeshLeaseSweepBackgroundService>? _logger;

    public MeshLeaseSweepBackgroundService(
        IMeshTaskRegistry tasks,
        IOptionsMonitor<MeshCheckpointOptions> options,
        ILogger<MeshLeaseSweepBackgroundService>? logger = null)
    {
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var opts = _options.CurrentValue;
            var interval = TimeSpan.FromMinutes(Math.Max(1, opts.SweepIntervalMinutes));

            try
            {
                if (opts.SweepEnabled)
                {
                    var now = DateTimeOffset.UtcNow;
                    var list = await _tasks.ListAsync(stoppingToken).ConfigureAwait(false);
                    foreach (var t in list)
                    {
                        if (t.Status is not (MeshTaskStatus.Assigned or MeshTaskStatus.Running))
                            continue;
                        if (t.LeaseExpiresUtc is null || t.LeaseExpiresUtc > now)
                            continue;

                        var cleared = t with
                        {
                            Status = MeshTaskStatus.Pending,
                            AssignedPeerId = null,
                            AssignedApiBaseUrl = null,
                            LeaseToken = null,
                            LeaseOwnerPeerId = null,
                            LeaseExpiresUtc = null,
                            PlacementReason = "lease.expired"
                        };
                        await _tasks.UpdateAsync(cleared, stoppingToken).ConfigureAwait(false);
                        _logger?.LogWarning("mesh-lease-sweep reclaimed task={TaskId}", t.TaskId);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "mesh-lease-sweep round failed");
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
