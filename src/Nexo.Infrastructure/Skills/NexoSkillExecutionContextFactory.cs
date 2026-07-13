using Nexo.Abstractions.Barriers;
using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;
using Nexo.Core.Application.Skills.Ports;
using Nexo.Core.Application.Trust.Ports;

namespace Nexo.Infrastructure.Skills;

/// <summary>
/// Builds skill execution context from barrier identity and active policy pack.
/// </summary>
public sealed class NexoSkillExecutionContextFactory : INexoSkillExecutionContextFactory
{
    private readonly IBarrierContextAccessor _barrierContextAccessor;
    private readonly ITrustPolicyPackRegistry _policyPackRegistry;

    /// <summary>Initializes a new skill execution context factory.</summary>
    public NexoSkillExecutionContextFactory(
        IBarrierContextAccessor barrierContextAccessor,
        ITrustPolicyPackRegistry policyPackRegistry)
    {
        _barrierContextAccessor = barrierContextAccessor;
        _policyPackRegistry = policyPackRegistry;
    }

    /// <inheritdoc />
    public NexoSkillExecutionContext Create(
        PeerTrustTier trustTier,
        string? actingIdentity = null,
        string? correlationId = null)
    {
        var barrier = _barrierContextAccessor.Current;
        var activePack = _policyPackRegistry.GetActivePack();

        return new NexoSkillExecutionContext(
            ActingIdentity: actingIdentity ?? barrier?.IssuedTo ?? "unknown",
            BarrierLevel: barrier?.Level ?? "public",
            TrustTier: trustTier,
            PolicyPackId: activePack?.Id,
            CorrelationId: correlationId ?? barrier?.CorrelationId ?? Guid.NewGuid().ToString("N"));
    }
}
