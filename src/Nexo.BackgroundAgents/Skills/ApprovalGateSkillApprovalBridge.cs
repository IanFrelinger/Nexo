using Nexo.BackgroundAgents.Configuration;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;

namespace Nexo.BackgroundAgents.Skills;

/// <summary>
/// Bridges background-agent approval gates to Nexo skill script approval.
/// </summary>
public sealed class ApprovalGateSkillApprovalBridge : INexoSkillApprovalGate
{
    private readonly IApprovalGate _approvalGate;

    /// <summary>Initializes a new approval gate bridge.</summary>
    public ApprovalGateSkillApprovalBridge(IApprovalGate approvalGate)
        => _approvalGate = approvalGate;

    /// <inheritdoc />
    public async Task<NexoSkillApprovalStatus> RequestApprovalAsync(
        string actionDescription,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var result = await _approvalGate.RequestApprovalAsync(actionDescription, timeout, cancellationToken)
            .ConfigureAwait(false);

        return result switch
        {
            ApprovalResult.Approved => NexoSkillApprovalStatus.Approved,
            ApprovalResult.TimedOut => NexoSkillApprovalStatus.TimedOut,
            _ => NexoSkillApprovalStatus.Denied,
        };
    }
}
