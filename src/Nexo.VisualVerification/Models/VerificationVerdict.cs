namespace Nexo.VisualVerification;

/// <summary>Harness verdict comparing captured frames against golden or determinism checks.</summary>
public sealed record VerificationVerdict(
    VerdictTier Tier,
    IReadOnlyList<int> MismatchedSteps,
    double WorstPerceptualDistance);
