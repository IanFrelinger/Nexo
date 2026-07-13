using System.Collections.Concurrent;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// In-memory pending approval store with waitable resolution for human-in-the-loop.
/// </summary>
public sealed class InMemorySkillApprovalStore : INexoSkillApprovalStore
{
    private readonly ConcurrentDictionary<string, NexoSkillApprovalRequest> _requests = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<NexoSkillApprovalStatus>> _waiters = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public NexoSkillApprovalRequest RegisterPending(SkillScriptApprovalKey key, string description)
    {
        var request = new NexoSkillApprovalRequest(
            Guid.NewGuid().ToString("N"),
            key,
            description,
            DateTimeOffset.UtcNow,
            NexoSkillApprovalStatus.Pending);

        _requests[request.RequestId] = request;
        _waiters[request.RequestId] = new TaskCompletionSource<NexoSkillApprovalStatus>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        return request;
    }

    /// <inheritdoc />
    public bool TryGet(string requestId, out NexoSkillApprovalRequest? request)
        => _requests.TryGetValue(requestId, out request);

    /// <inheritdoc />
    public bool TryResolve(string requestId, NexoSkillApprovalStatus status, out NexoSkillApprovalRequest? request)
    {
        request = null;
        if (status is NexoSkillApprovalStatus.Pending or NexoSkillApprovalStatus.AutoApproved)
            return false;

        if (!_requests.TryGetValue(requestId, out var existing))
            return false;

        if (existing.Status != NexoSkillApprovalStatus.Pending)
        {
            request = existing;
            return false;
        }

        request = existing with { Status = status };
        _requests[requestId] = request;

        if (_waiters.TryRemove(requestId, out var waiter))
            waiter.TrySetResult(status);

        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<NexoSkillApprovalRequest> GetPending()
        => _requests.Values
            .Where(static request => request.Status == NexoSkillApprovalStatus.Pending)
            .OrderBy(static request => request.RequestedAt)
            .ToList();

    /// <inheritdoc />
    public async Task<NexoSkillApprovalStatus> WaitForResolutionAsync(
        string requestId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!_requests.TryGetValue(requestId, out var existing))
            return NexoSkillApprovalStatus.Denied;

        if (existing.Status != NexoSkillApprovalStatus.Pending)
            return existing.Status;

        if (!_waiters.TryGetValue(requestId, out var waiter))
            return NexoSkillApprovalStatus.Denied;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            return await waiter.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryResolve(requestId, NexoSkillApprovalStatus.TimedOut, out _);
            return NexoSkillApprovalStatus.TimedOut;
        }
    }
}
