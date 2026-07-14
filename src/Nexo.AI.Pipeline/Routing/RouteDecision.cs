namespace Nexo.AI.Pipeline.Routing;

/// <summary>
/// Capability / cost tier requested by a caller (via <see cref="Microsoft.Extensions.AI.ChatOptions"/>).
/// </summary>
public enum RouteCapabilityTier
{
    /// <summary>Default / cheapest local-capable work.</summary>
    Fast = 0,

    /// <summary>Balanced quality/latency.</summary>
    Balanced = 1,

    /// <summary>Heavy reasoning / large context — may escalate to cloud when allowed.</summary>
    Heavy = 2,
}

/// <summary>
/// Why a particular candidate was chosen or skipped.
/// </summary>
public static class RouteReasonCodes
{
    public const string PreferredLocal = "preferred_local";
    public const string LocalUnavailable = "local_unavailable";
    public const string PolicyDenied = "policy_denied";
    public const string CapabilityEscalation = "capability_escalation";
    public const string Fallback = "fallback";
    public const string NoCandidate = "no_candidate";
}

/// <summary>
/// Snapshot of a routing decision for audit.
/// </summary>
public sealed class RouteDecision
{
    public required string ChosenTargetKey { get; init; }
    public required string ReasonCode { get; init; }
    public required IReadOnlyList<RouteCandidate> CandidatesConsidered { get; init; }
    public RouteCapabilityTier RequestedTier { get; init; }
}

/// <summary>
/// One candidate evaluated during routing.
/// </summary>
public sealed class RouteCandidate
{
    public required string TargetKey { get; init; }
    public required bool Available { get; init; }
    public required bool PolicyAllowed { get; init; }
    public required string Disposition { get; init; }
}
