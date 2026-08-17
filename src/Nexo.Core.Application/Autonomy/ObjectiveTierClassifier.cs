using System.Diagnostics.CodeAnalysis;

namespace Nexo.Core.Application.Autonomy;

/// <summary>Admission tier of one objective (autonomous self-extension spec §3).</summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public enum ObjectiveTier
{
    /// <summary>Leaf-brick blast radius: certified artifacts may hot-swap without human approval.</summary>
    Tier0Autonomous = 0,

    /// <summary>Certification may proceed autonomously; ADMISSION waits for the human gate.</summary>
    Tier1HumanAdmission = 1,

    /// <summary>The OBJECTIVE itself must originate from a human; machine sources may not open a session.</summary>
    Tier2HumanObjective = 2,
}

/// <summary>One classification verdict: the tier plus every reason that produced it.</summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public sealed record TierClassification(ObjectiveTier Tier, IReadOnlyList<string> Reasons)
{
    /// <summary>
    /// Whether a proposal session may open for this objective given its source (R3.1
    /// Tier 2: machine sources must not even open a session against authority rules).
    /// </summary>
    public bool MaySessionOpen(ObjectiveSource source) =>
        Tier != ObjectiveTier.Tier2HumanObjective || source == ObjectiveSource.Human;
}

/// <summary>
/// Classifies objectives by touch-set ONLY (I-2: autonomy is bounded by blast radius,
/// never by confidence, scores, or history — Tier 0 is not earnable, R3.3). The
/// classifier is deliberately a pure function of the declaration; it never consults gate
/// results, success streaks, or proposer self-assessment.
/// </summary>
[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]
public static class ObjectiveTierClassifier
{
    /// <summary>Classifies one declared touch-set (R3.1) fail-closed (R1.4).</summary>
    public static TierClassification Classify(TouchSet touchSet)
    {
        if (touchSet is null)
            throw new ArgumentNullException(nameof(touchSet));
        var normalized = touchSet.Normalize();

        if (!normalized.IsDeclared)
        {
            // R1.4: an objective whose blast radius cannot be statically determined is
            // classified at the most restrictive applicable tier. Tier 2 gates objective
            // ORIGIN for authority-rule changes, which an undeclared set cannot name —
            // and its session has no writable surfaces anyway — so Tier 1 applies.
            return new TierClassification(
                ObjectiveTier.Tier1HumanAdmission,
                new[] { "touch-set is undeclared; blast radius cannot be statically determined (R1.4)" });
        }

        var authorityHits = normalized.PathPrefixes
            .Where(TrustKernel.IsAuthorityRulePath)
            .Select(p => $"authority-rule path {p}")
            .ToArray();
        if (authorityHits.Length > 0)
        {
            return new TierClassification(ObjectiveTier.Tier2HumanObjective, authorityHits);
        }

        var kernelHits = TrustKernel.KernelIntersections(normalized);
        if (kernelHits.Count > 0)
        {
            return new TierClassification(ObjectiveTier.Tier1HumanAdmission, kernelHits);
        }

        return new TierClassification(
            ObjectiveTier.Tier0Autonomous,
            new[] { "declared touch-set does not reach the trust kernel" });
    }
}
