namespace Nexo.VisualVerification;

/// <summary>Compares captured frames against golden fingerprints and computes verdict tiers.</summary>
public static class VerdictComputer
{
    public static VerificationVerdict CompareToExpectations(
        string scenarioHash,
        string buildArtifactSha256,
        IReadOnlyList<CapturedFrame> capturedFrames,
        FrameExpectationSet expectations,
        int perceptualEpsilon = PerceptualHash.DefaultEpsilon)
    {
        if (!string.Equals(scenarioHash, expectations.ScenarioHash, StringComparison.Ordinal)
            || !string.Equals(buildArtifactSha256, expectations.BuildArtifactSha256, StringComparison.Ordinal))
        {
            return new VerificationVerdict(
                VerdictTier.Mismatch,
                capturedFrames.Select(f => f.Step).ToArray(),
                0);
        }

        return CompareFrames(capturedFrames, expectations.Frames, perceptualEpsilon);
    }

    public static VerificationVerdict CompareToGolden(
        IReadOnlyList<CapturedFrame> capturedFrames,
        GoldenEyeTestRecord golden,
        int perceptualEpsilon = PerceptualHash.DefaultEpsilon) =>
        CompareFrames(
            capturedFrames,
            golden.Frames.Select(f => new FrameFingerprint(f.Step, f.Sha256, f.PerceptualHash)).ToArray(),
            perceptualEpsilon);

    private static VerificationVerdict CompareFrames(
        IReadOnlyList<CapturedFrame> capturedFrames,
        IReadOnlyList<FrameFingerprint> expectations,
        int perceptualEpsilon)
    {
        var mismatched = new List<int>();
        var worstDistance = 0.0;
        var exact = true;
        var perceptual = true;

        var goldenByStep = expectations.ToDictionary(f => f.Step);
        foreach (var frame in capturedFrames.OrderBy(f => f.Step))
        {
            if (!goldenByStep.TryGetValue(frame.Step, out var goldenFrame))
            {
                mismatched.Add(frame.Step);
                exact = false;
                perceptual = false;
                continue;
            }

            if (!string.Equals(frame.Sha256, goldenFrame.Sha256, StringComparison.Ordinal))
            {
                exact = false;
                var distance = PerceptualHash.HammingDistance(frame.PerceptualHash, goldenFrame.PerceptualHash);
                worstDistance = Math.Max(worstDistance, distance);
                if (distance > perceptualEpsilon)
                {
                    perceptual = false;
                    mismatched.Add(frame.Step);
                }
            }
        }

        if (exact)
            return new VerificationVerdict(VerdictTier.ExactMatch, Array.Empty<int>(), 0);

        if (perceptual)
            return new VerificationVerdict(VerdictTier.PerceptualMatch, Array.Empty<int>(), worstDistance);

        return new VerificationVerdict(VerdictTier.Mismatch, mismatched, worstDistance);
    }

    public static VerificationVerdict NonDeterministic(IReadOnlyList<int> mismatchedSteps) =>
        new(VerdictTier.NonDeterministic, mismatchedSteps, 0);

    public static VerificationVerdict CaptureFailure() =>
        new(VerdictTier.CaptureFailure, Array.Empty<int>(), 0);
}
