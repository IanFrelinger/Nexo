using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Commercial.Fleet.Contracts.Models;
using Ashlar.Commercial.Fleet.Contracts.Ports;

namespace Ashlar.Commercial.Fleet.Infrastructure;

/// <summary>
/// Phase 6: lease extension and migrate-for-checkpoint helpers.
/// </summary>
public sealed class MeshTaskExecutionService
{
    private readonly IMeshTaskRegistry _tasks;
    private readonly IOptions<MeshCheckpointOptions> _checkpointOptions;
    private readonly ILogger<MeshTaskExecutionService>? _logger;

    public MeshTaskExecutionService(
        IMeshTaskRegistry tasks,
        IOptions<MeshCheckpointOptions> checkpointOptions,
        ILogger<MeshTaskExecutionService>? logger = null)
    {
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _checkpointOptions = checkpointOptions ?? throw new ArgumentNullException(nameof(checkpointOptions));
        _logger = logger;
    }

    public async Task<(bool Ok, MeshTaskState? Task, string? Error)> ExtendLeaseAsync(
        string taskId,
        string leaseToken,
        int? extendSeconds,
        CancellationToken cancellationToken = default)
    {
        var t = await _tasks.GetAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return (false, null, "task.not_found");
        if (string.IsNullOrWhiteSpace(t.LeaseToken) || !SecureEquals(t.LeaseToken, leaseToken))
            return (false, t, "lease.invalid_token");

        var baseSeconds = Math.Max(60, _checkpointOptions.Value.LeaseSeconds);
        var add = extendSeconds.HasValue ? Math.Clamp(extendSeconds.Value, 60, 86_400) : baseSeconds;
        var newExpiry = DateTimeOffset.UtcNow.AddSeconds(add);
        var next = t with { LeaseExpiresUtc = newExpiry };
        await _tasks.UpdateAsync(next, cancellationToken).ConfigureAwait(false);
        _logger?.LogDebug("mesh-lease extended task={TaskId} until={Until}", taskId, newExpiry);
        return (true, next, null);
    }

    public async Task<(bool Ok, MeshTaskState? Task, string? Error)> MigrateForCheckpointAsync(
        string taskId,
        string leaseToken,
        string checkpointHandle,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(checkpointHandle))
            return (false, null, "checkpoint.required");

        var t = await _tasks.GetAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (t is null)
            return (false, null, "task.not_found");
        if (string.IsNullOrWhiteSpace(t.LeaseToken) || !SecureEquals(t.LeaseToken, leaseToken))
            return (false, t, "lease.invalid_token");
        if (t.Status is not (MeshTaskStatus.Assigned or MeshTaskStatus.Running))
            return (false, t, "migrate.invalid_status");

        var next = t with
        {
            Status = MeshTaskStatus.Pending,
            AssignedPeerId = null,
            AssignedApiBaseUrl = null,
            CheckpointHandle = checkpointHandle.Trim(),
            LeaseToken = null,
            LeaseOwnerPeerId = null,
            LeaseExpiresUtc = null,
            PlacementReason = "migrate.checkpoint_ready"
        };
        await _tasks.UpdateAsync(next, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation("mesh-migrate task={TaskId} checkpoint recorded", taskId);
        return (true, next, null);
    }

    private static bool SecureEquals(string a, string b)
    {
        var aa = Encoding.UTF8.GetBytes(a.Trim());
        var bb = Encoding.UTF8.GetBytes(b.Trim());
        return aa.Length == bb.Length && CryptographicOperations.FixedTimeEquals(aa, bb);
    }
}
