namespace Ashlar.Commercial.GameDomain.Discord;

/// <summary>Operator approval action parsed from Discord playtest review interactions.</summary>
public enum ApprovalAction
{
    /// <summary>Approve all proposed playtest fixes.</summary>
    ApproveAll,
    /// <summary>Approve a single selected fix.</summary>
    ApproveSingle,
    /// <summary>Reject all proposed playtest fixes.</summary>
    RejectAll,
    /// <summary>Unrecognized or unsupported approval action.</summary>
    Unknown
}
