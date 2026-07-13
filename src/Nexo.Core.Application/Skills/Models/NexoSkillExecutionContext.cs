using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Skills.Models;

/// <summary>
/// Trust and identity context applied when resolving skill visibility and execution.
/// </summary>
public sealed record NexoSkillExecutionContext(
    string ActingIdentity,
    string BarrierLevel,
    PeerTrustTier TrustTier,
    string? PolicyPackId,
    string CorrelationId);
