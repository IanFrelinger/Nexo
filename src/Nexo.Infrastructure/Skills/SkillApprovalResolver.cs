using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Resolves skill script approval via policy auto-approve or the human-in-the-loop gate.
/// </summary>
public sealed class SkillApprovalResolver : INexoSkillApprovalResolver
{
    private readonly INexoSkillPolicyEvaluator _policyEvaluator;
    private readonly INexoSkillApprovalStore _approvalStore;
    private readonly INexoSkillApprovalGate _approvalGate;
    private readonly TimeSpan _approvalTimeout;

    /// <summary>Initializes a new skill approval resolver.</summary>
    public SkillApprovalResolver(
        INexoSkillPolicyEvaluator policyEvaluator,
        INexoSkillApprovalStore approvalStore,
        INexoSkillApprovalGate approvalGate,
        TimeSpan? approvalTimeout = null)
    {
        _policyEvaluator = policyEvaluator;
        _approvalStore = approvalStore;
        _approvalGate = approvalGate;
        _approvalTimeout = approvalTimeout ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    public async Task<NexoSkillApprovalStatus> ResolveScriptApprovalAsync(
        SkillScriptApprovalKey key,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (_policyEvaluator.IsScriptAutoApproved(key.SkillName, key.ScriptPath, CreateContext(key)))
            return NexoSkillApprovalStatus.AutoApproved;

        _approvalStore.RegisterPending(key, description);
        return await _approvalGate.RequestApprovalAsync(description, _approvalTimeout, cancellationToken)
            .ConfigureAwait(false);
    }

    private static NexoSkillExecutionContext CreateContext(SkillScriptApprovalKey key)
        => new(
            ActingIdentity: "skill-approval",
            BarrierLevel: key.BarrierLevel,
            TrustTier: key.TrustTier,
            PolicyPackId: key.PolicyPackId,
            CorrelationId: Guid.NewGuid().ToString("N"));
}
