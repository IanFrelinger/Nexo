using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Stores pending skill script approvals for operator visibility.
/// </summary>
public interface INexoSkillApprovalStore
{
    NexoSkillApprovalRequest RegisterPending(SkillScriptApprovalKey key, string description);

    bool TryResolve(string requestId, NexoSkillApprovalStatus status, out NexoSkillApprovalRequest? request);

    IReadOnlyList<NexoSkillApprovalRequest> GetPending();
}
