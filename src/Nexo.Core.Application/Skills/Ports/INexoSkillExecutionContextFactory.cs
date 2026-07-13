using Nexo.Core.Application.Mesh.Models;
using Nexo.Core.Application.Skills.Models;

namespace Nexo.Core.Application.Skills.Ports;

/// <summary>
/// Builds <see cref="NexoSkillExecutionContext"/> from ambient runtime state.
/// </summary>
public interface INexoSkillExecutionContextFactory
{
    NexoSkillExecutionContext Create(
        PeerTrustTier trustTier,
        string? actingIdentity = null,
        string? correlationId = null);
}
