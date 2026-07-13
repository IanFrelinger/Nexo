namespace Nexo.API.Endpoints;

/// <summary>Response for GET /api/skills.</summary>
public sealed record SkillListResponse(
    string TrustTier,
    IReadOnlyList<SkillDescriptorItem> Skills);

/// <summary>Skill advertisement item.</summary>
public sealed record SkillDescriptorItem(string Name, string Description);

/// <summary>Response for GET /api/skills/approvals.</summary>
public sealed record SkillApprovalsResponse(IReadOnlyList<SkillApprovalItem> Pending);

/// <summary>Pending or resolved skill script approval.</summary>
public sealed record SkillApprovalItem(
    string RequestId,
    string SkillName,
    string ScriptPath,
    string TrustTier,
    string BarrierLevel,
    string? PolicyPackId,
    string Description,
    string Status,
    DateTimeOffset RequestedAt);

/// <summary>Request body for POST /api/skills/approvals/{id}/resolve.</summary>
public sealed record ResolveSkillApprovalRequest(string Decision);
