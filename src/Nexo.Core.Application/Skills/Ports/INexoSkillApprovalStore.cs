using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Stores pending skill script approvals for operator visibility and human-in-the-loop waits.
/// </summary>
public interface INexoSkillApprovalStore
{
    NexoSkillApprovalRequest RegisterPending(SkillScriptApprovalKey key, string description);

    bool TryResolve(string requestId, NexoSkillApprovalStatus status, out NexoSkillApprovalRequest? request);

    bool TryGet(string requestId, out NexoSkillApprovalRequest? request);

    IReadOnlyList<NexoSkillApprovalRequest> GetPending();

    /// <summary>
    /// Blocks until an operator resolves the request or the timeout elapses.
    /// </summary>
    Task<NexoSkillApprovalStatus> WaitForResolutionAsync(
        string requestId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
