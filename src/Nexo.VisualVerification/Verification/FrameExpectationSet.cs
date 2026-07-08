namespace Nexo.VisualVerification;

/// <summary>Expected frame fingerprints for harness comparison (witness-independent of golden storage).</summary>
public sealed record FrameFingerprint(int Step, string Sha256, string PerceptualHash);

/// <summary>Frame expectations supplied to the harness without referencing golden storage types.</summary>
public sealed record FrameExpectationSet(
    string ScenarioHash,
    string BuildArtifactSha256,
    DisplayConfig Display,
    IReadOnlyList<FrameFingerprint> Frames);
