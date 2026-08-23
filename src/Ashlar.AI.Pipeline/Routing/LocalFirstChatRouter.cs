using Ashlar.AI.Pipeline.Governance;

namespace Ashlar.AI.Pipeline.Routing;

/// <summary>
/// Local-first router: prefers healthy policy-allowed local targets, then escalates.
/// </summary>
public sealed class LocalFirstChatRouter : IChatRouter
{
    private readonly IRouteCandidateTable _table;
    private readonly ITargetAvailability _availability;
    private readonly IChatTargetAccessPolicy _policy;

    /// <summary>Creates a local-first router.</summary>
    public LocalFirstChatRouter(
        IRouteCandidateTable table,
        ITargetAvailability availability,
        IChatTargetAccessPolicy policy)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _availability = availability ?? throw new ArgumentNullException(nameof(availability));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <inheritdoc />
    public RouteDecision Select(
        string? callerIdentity,
        string? modelId,
        RouteCapabilityTier tier,
        string? preferredTarget)
    {
        var cloudPermitted = IsCloudPermitted(callerIdentity, modelId);
        var ordered = BuildOrderedCandidates(tier, cloudPermitted, preferredTarget);
        var considered = new List<RouteCandidate>(ordered.Count);

        foreach (var key in ordered)
        {
            var available = _availability.IsAvailable(key);
            var allowed = _policy.IsAllowed(callerIdentity, key, modelId, out var denyReason);
            string disposition;
            if (!allowed)
            {
                disposition = $"skip:{RouteReasonCodes.PolicyDenied}:{denyReason}";
                considered.Add(new RouteCandidate
                {
                    TargetKey = key,
                    Available = available,
                    PolicyAllowed = false,
                    Disposition = disposition,
                });
                continue;
            }

            if (!available)
            {
                disposition = $"skip:{RouteReasonCodes.LocalUnavailable}";
                considered.Add(new RouteCandidate
                {
                    TargetKey = key,
                    Available = false,
                    PolicyAllowed = true,
                    Disposition = disposition,
                });
                continue;
            }

            var reason = ResolveReason(key, considered, preferredTarget, tier);
            considered.Add(new RouteCandidate
            {
                TargetKey = key,
                Available = true,
                PolicyAllowed = true,
                Disposition = $"choose:{reason}",
            });

            return new RouteDecision
            {
                ChosenTargetKey = key,
                ReasonCode = reason,
                CandidatesConsidered = considered,
                RequestedTier = tier,
            };
        }

        return new RouteDecision
        {
            ChosenTargetKey = string.Empty,
            ReasonCode = RouteReasonCodes.NoCandidate,
            CandidatesConsidered = considered,
            RequestedTier = tier,
        };
    }

    private bool IsCloudPermitted(string? callerIdentity, string? modelId)
    {
        // If any cloud target is policy-allowed, treat cloud as permitted for candidate inclusion.
        foreach (var cloud in new[]
                 {
                     DefaultRouteCandidateTable.CloudBedrockFast,
                     DefaultRouteCandidateTable.CloudBedrockBalanced,
                     DefaultRouteCandidateTable.CloudBedrockHeavy,
                 })
        {
            if (_policy.IsAllowed(callerIdentity, cloud, modelId, out _))
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<string> BuildOrderedCandidates(
        RouteCapabilityTier tier,
        bool cloudPermitted,
        string? preferredTarget)
    {
        var baseList = _table.GetCandidates(tier, cloudPermitted).ToList();
        if (string.IsNullOrWhiteSpace(preferredTarget))
        {
            return baseList;
        }

        baseList.RemoveAll(k => string.Equals(k, preferredTarget, StringComparison.OrdinalIgnoreCase));
        baseList.Insert(0, preferredTarget);
        return baseList;
    }

    private static string ResolveReason(
        string chosen,
        IReadOnlyList<RouteCandidate> prior,
        string? preferredTarget,
        RouteCapabilityTier tier)
    {
        if (!string.IsNullOrWhiteSpace(preferredTarget)
            && string.Equals(chosen, preferredTarget, StringComparison.OrdinalIgnoreCase)
            && prior.Count == 0)
        {
            return RouteReasonCodes.PreferredLocal;
        }

        if (chosen.StartsWith("local:", StringComparison.OrdinalIgnoreCase) && prior.Count == 0)
        {
            return RouteReasonCodes.PreferredLocal;
        }

        if (chosen.StartsWith("cloud:", StringComparison.OrdinalIgnoreCase))
        {
            return tier == RouteCapabilityTier.Heavy
                ? RouteReasonCodes.CapabilityEscalation
                : RouteReasonCodes.Fallback;
        }

        return prior.Count == 0 ? RouteReasonCodes.PreferredLocal : RouteReasonCodes.Fallback;
    }
}
