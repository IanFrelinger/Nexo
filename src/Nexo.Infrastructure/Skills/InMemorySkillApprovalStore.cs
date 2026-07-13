using System.Collections.Concurrent;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// In-memory pending approval store for skill script execution.
/// </summary>
public sealed class InMemorySkillApprovalStore : INexoSkillApprovalStore
{
    private readonly ConcurrentDictionary<string, NexoSkillApprovalRequest> _requests = new(StringComparer.OrdinalIgnoreCase);

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
        return request;
    }

    /// <inheritdoc />
    public bool TryResolve(string requestId, NexoSkillApprovalStatus status, out NexoSkillApprovalRequest? request)
    {
        request = null;
        if (!_requests.TryGetValue(requestId, out var existing))
            return false;

        request = existing with { Status = status };
        _requests[requestId] = request;
        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<NexoSkillApprovalRequest> GetPending()
        => _requests.Values
            .Where(static request => request.Status == NexoSkillApprovalStatus.Pending)
            .OrderBy(static request => request.RequestedAt)
            .ToList();
}
