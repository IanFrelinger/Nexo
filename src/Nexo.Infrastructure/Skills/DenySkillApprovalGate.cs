using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Default gate that denies all skill script approvals.
/// </summary>
public sealed class DenySkillApprovalGate : INexoSkillApprovalGate
{
    /// <inheritdoc />
    public Task<NexoSkillApprovalStatus> RequestApprovalAsync(
        string actionDescription,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NexoSkillApprovalStatus.Denied);
}
