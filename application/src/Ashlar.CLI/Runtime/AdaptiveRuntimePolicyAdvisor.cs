namespace Ashlar.CLI.Runtime;

/// <summary>Adaptive runtime policy advisor.</summary>
public static class AdaptiveRuntimePolicyAdvisor
{
    /// <summary>Creates a new RecommendQaPolicy instance.</summary>
    public static AdaptiveRuntimePolicyRecommendation? RecommendQaPolicy(
        string goal,
        IReadOnlyList<AdaptiveRuntimeExecutionReport> history)
    {
        var fingerprint = AdaptiveRuntimeExecutionReport.ComputeGoalFingerprint(goal);
        var relevant = history
            .Where(h => string.Equals(h.GoalFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(h => h.StartedAtUtc)
            .Take(8)
            .ToArray();

        if (relevant.Length == 0)
            return null;

        var preflightFailures = relevant.Count(h => !h.Success && string.Equals(h.FailureStage, "preflight", StringComparison.OrdinalIgnoreCase));
        if (preflightFailures >= 2 && relevant.Any(h => h.RunVisualQa))
        {
            return new AdaptiveRuntimePolicyRecommendation(
                "demo",
                "Observed repeated visual preflight failures for this objective; recommending demo policy to allow degrade fallback.");
        }

        var selfExtendFailures = relevant.Count(h => !h.Success && string.Equals(h.FailureStage, "self-extend", StringComparison.OrdinalIgnoreCase));
        if (selfExtendFailures >= 2)
        {
            return new AdaptiveRuntimePolicyRecommendation(
                "research",
                "Observed repeated self-extend QA failures for this objective; recommending research policy for deeper iterative retries.");
        }

        var demoSuccesses = relevant.Count(h => h.Success && string.Equals(h.ResolvedQaPolicy, "demo", StringComparison.OrdinalIgnoreCase));
        if (demoSuccesses >= 3)
        {
            return new AdaptiveRuntimePolicyRecommendation(
                "demo",
                "Recent demo-policy runs for this objective are stable; keeping fast demo policy.");
        }

        return null;
    }
}
