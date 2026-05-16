using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexo.Core.Application.Fleet.Models;
using Nexo.Core.Application.Fleet.Ports;

namespace Nexo.Infrastructure.Fleet;

/// <summary>
/// Greedy placement: eligible non-drained nodes that advertise required bricks and match affinity labels.
/// </summary>
public sealed class MeshTaskPlacementService : IMeshTaskPlacementService
{
    private readonly IFleetNodeRegistry _nodes;
    private readonly IMeshTaskRegistry _tasks;
    private readonly MeshCheckpointOptions _checkpointOptions;
    private readonly ILogger<MeshTaskPlacementService>? _logger;

    public MeshTaskPlacementService(
        IFleetNodeRegistry nodes,
        IMeshTaskRegistry tasks,
        IOptions<MeshCheckpointOptions> checkpointOptions,
        ILogger<MeshTaskPlacementService>? logger = null)
    {
        _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        _tasks = tasks ?? throw new ArgumentNullException(nameof(tasks));
        _checkpointOptions = checkpointOptions?.Value ?? throw new ArgumentNullException(nameof(checkpointOptions));
        _logger = logger;
    }

    public Task<(bool Ok, MeshTaskState? Task, string? Error)> TryScheduleAsync(
        string taskId,
        string? scheduleIdempotencyKey = null,
        string? correlationId = null,
        int? leaseSecondsOverride = null,
        CancellationToken cancellationToken = default)
        => TryPlaceAsync(taskId, skipPreviousPeerId: null, scheduleIdempotencyKey, correlationId, leaseSecondsOverride, cancellationToken);

    public async Task<(bool Ok, MeshTaskState? Task, string? Error)> TryRetryAsync(
        string taskId,
        string? scheduleIdempotencyKey = null,
        string? correlationId = null,
        int? leaseSecondsOverride = null,
        CancellationToken cancellationToken = default)
    {
        var current = await _tasks.GetAsync(taskId, cancellationToken).ConfigureAwait(false);
        var skip = current?.AssignedPeerId;
        return await TryPlaceAsync(taskId, skipPreviousPeerId: skip, scheduleIdempotencyKey, correlationId, leaseSecondsOverride, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(bool Ok, MeshTaskState? Task, string? Error)> TryPlaceAsync(
        string taskId,
        string? skipPreviousPeerId,
        string? scheduleIdempotencyKey,
        string? correlationId,
        int? leaseSecondsOverride,
        CancellationToken cancellationToken)
    {
        var task = await _tasks.GetAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task is null)
            return (false, null, "task.not_found");

        if (task.Status == MeshTaskStatus.Succeeded)
            return (false, task, "task.already_succeeded");

        if (task.DeadlineUtc is { } d && DateTimeOffset.UtcNow > d)
        {
            var expired = task with { Status = MeshTaskStatus.Failed, PlacementReason = "deadline_exceeded" };
            await _tasks.UpdateAsync(expired, cancellationToken).ConfigureAwait(false);
            return (false, expired, "task.deadline_exceeded");
        }

        var isRetry = !string.IsNullOrEmpty(skipPreviousPeerId);
        var scheduleKey = string.IsNullOrWhiteSpace(scheduleIdempotencyKey) ? null : scheduleIdempotencyKey.Trim();
        var corr = string.IsNullOrWhiteSpace(correlationId)
            ? task.CorrelationId
            : correlationId.Trim();

        // Active lease + idempotent schedule (Phase 3) or reclaim expired lease (Phase 6).
        if ((task.Status == MeshTaskStatus.Assigned || task.Status == MeshTaskStatus.Running) &&
            !string.IsNullOrEmpty(task.AssignedPeerId))
        {
            var nowUtc = DateTimeOffset.UtcNow;
            if (task.LeaseExpiresUtc is { } exp && nowUtc > exp)
            {
                var reclaimed = task with
                {
                    Status = MeshTaskStatus.Pending,
                    AssignedPeerId = null,
                    AssignedApiBaseUrl = null,
                    LeaseToken = null,
                    LeaseOwnerPeerId = null,
                    LeaseExpiresUtc = null,
                    PlacementReason = "lease.expired_before_reschedule"
                };
                await _tasks.UpdateAsync(reclaimed, cancellationToken).ConfigureAwait(false);
                task = await _tasks.GetAsync(taskId, cancellationToken).ConfigureAwait(false) ?? reclaimed;
            }
            else if (!isRetry && task.Status == MeshTaskStatus.Assigned)
            {
                if (scheduleKey is null)
                    return (true, task with { CorrelationId = corr ?? task.CorrelationId }, null);
                if (string.Equals(task.LastScheduleIdempotencyKey, scheduleKey, StringComparison.Ordinal))
                    return (true, task with { CorrelationId = corr ?? task.CorrelationId }, null);
                return (false, task with { CorrelationId = corr ?? task.CorrelationId }, "schedule.idempotency_conflict");
            }
            else if (!isRetry && task.Status == MeshTaskStatus.Running)
            {
                if (scheduleKey is null)
                    return (true, task with { CorrelationId = corr ?? task.CorrelationId }, null);
                if (string.Equals(task.LastScheduleIdempotencyKey, scheduleKey, StringComparison.Ordinal))
                    return (true, task with { CorrelationId = corr ?? task.CorrelationId }, null);
                return (false, task with { CorrelationId = corr ?? task.CorrelationId }, "schedule.idempotency_conflict");
            }
        }

        var work = isRetry
            ? task with
            {
                Status = MeshTaskStatus.Pending,
                AssignedPeerId = null,
                AssignedApiBaseUrl = null,
                PlacementReason = null,
                CorrelationId = corr ?? task.CorrelationId,
                LeaseToken = null,
                LeaseOwnerPeerId = null,
                LeaseExpiresUtc = null
            }
            : task with { CorrelationId = corr ?? task.CorrelationId };

        if (work.Status != MeshTaskStatus.Pending)
            work = work with { Status = MeshTaskStatus.Pending };

        var nodes = await _nodes.ListAsync(cancellationToken).ConfigureAwait(false);
        var eligible = nodes
            .Where(n => !n.Drained && !string.IsNullOrWhiteSpace(n.ApiBaseUrl))
            .Where(n => MatchesAffinity(n, work.Affinity))
            .Where(n => HasBricks(n, work.RequiredBrickIds))
            .OrderBy(n => n.ReportedQueueDepth)
            .ThenByDescending(n => n.LastHeartbeatUtc ?? DateTimeOffset.MinValue)
            .ThenBy(n => n.PeerId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (eligible.Count == 0)
        {
            const string reason = "placement.no_eligible_nodes";
            _logger?.LogWarning("mesh placement task={TaskId} reason={Reason}", taskId, reason);
            var pending = work with { Status = MeshTaskStatus.Pending, PlacementReason = reason };
            await _tasks.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
            return (false, pending, reason);
        }

        var candidates = string.IsNullOrEmpty(skipPreviousPeerId)
            ? eligible
            : eligible.Where(n => !string.Equals(n.PeerId, skipPreviousPeerId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0)
            candidates = eligible;

        var pick = candidates[0];
        var leaseSeconds = leaseSecondsOverride.HasValue
            ? Math.Clamp(leaseSecondsOverride.Value, 60, 86_400)
            : Math.Max(60, _checkpointOptions.LeaseSeconds);
        var leaseToken = Guid.NewGuid().ToString("N");
        var leaseUntil = DateTimeOffset.UtcNow.AddSeconds(leaseSeconds);

        var assigned = work with
        {
            Status = MeshTaskStatus.Assigned,
            AssignedPeerId = pick.PeerId,
            AssignedApiBaseUrl = NormalizeBaseUrl(pick.ApiBaseUrl),
            PlacementReason = "placement.ok",
            AttemptCount = work.AttemptCount + 1,
            LastScheduledAtUtc = DateTimeOffset.UtcNow,
            LastScheduleIdempotencyKey = scheduleKey,
            LeaseToken = leaseToken,
            LeaseOwnerPeerId = pick.PeerId,
            LeaseExpiresUtc = leaseUntil,
            CheckpointHandle = null
        };

        await _tasks.UpdateAsync(assigned, cancellationToken).ConfigureAwait(false);
        _logger?.LogInformation(
            "mesh placement task={TaskId} peer={PeerId} attempt={Attempt} retry={Retry} correlationId={CorrelationId}",
            taskId,
            pick.PeerId,
            assigned.AttemptCount,
            isRetry,
            assigned.CorrelationId ?? "");

        return (true, assigned, null);
    }

    private static string NormalizeBaseUrl(string url)
    {
        var t = url.Trim();
        return t.EndsWith('/') ? t : t + "/";
    }

    private static bool MatchesAffinity(MeshFleetNodeState node, IReadOnlyDictionary<string, string> affinity)
    {
        if (affinity.Count == 0) return true;
        foreach (var kv in affinity)
        {
            if (!node.Labels.TryGetValue(kv.Key, out var v) ||
                !string.Equals(v, kv.Value, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool HasBricks(MeshFleetNodeState node, IReadOnlyList<string> required)
    {
        if (required.Count == 0) return true;
        var caps = new HashSet<string>(node.AdvertisedBrickIds, StringComparer.OrdinalIgnoreCase);
        foreach (var label in node.Labels)
        {
            if (label.Value.StartsWith("brick:", StringComparison.OrdinalIgnoreCase))
                caps.Add(label.Value["brick:".Length..].Trim());
        }

        foreach (var r in required)
        {
            if (string.IsNullOrWhiteSpace(r)) continue;
            if (!caps.Contains(r.Trim())) return false;
        }

        return true;
    }
}
