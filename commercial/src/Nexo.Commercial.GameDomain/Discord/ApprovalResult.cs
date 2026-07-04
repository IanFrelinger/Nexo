namespace Nexo.Commercial.GameDomain.Discord;

/// <summary>Outcome of a Discord playtest approval interaction.</summary>
/// <param name="Action">Selected approval action.</param>
/// <param name="FixesToApply">Proposed fixes approved for application.</param>
/// <param name="Summary">Human-readable summary for operator logs.</param>
public sealed record ApprovalResult(
    ApprovalAction Action,
    IReadOnlyList<Playtest.ProposedFix> FixesToApply,
    string Summary);
