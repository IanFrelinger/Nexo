using Nexo.Core.Application.Mesh.Models;

namespace Nexo.Core.Application.Skills.Models;

/// <summary>Identity tuple for skill script approval decisions.</summary>
public sealed record SkillScriptApprovalKey(
    string SkillName,
    string ScriptPath,
    PeerTrustTier TrustTier,
    string BarrierLevel,
    string? PolicyPackId);
