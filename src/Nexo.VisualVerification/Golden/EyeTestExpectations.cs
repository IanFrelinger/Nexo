namespace Nexo.VisualVerification;

/// <summary>Converts certified sessions into harness frame expectations.</summary>
public static class EyeTestExpectations
{
    public static FrameExpectationSet ToExpectations(this EyeTestSessionRecord record) =>
        new(
            record.ScenarioHash,
            record.BuildArtifactSha256,
            record.Display,
            record.Frames.Select(f => new FrameFingerprint(f.Step, f.Sha256, f.PerceptualHash)).ToArray());

    public static FrameExpectationSet ToExpectations(this GoldenEyeTestRecord golden) =>
        new(
            golden.ScenarioHash,
            golden.BuildArtifactSha256,
            golden.Display,
            golden.Frames.Select(f => new FrameFingerprint(f.Step, f.Sha256, f.PerceptualHash)).ToArray());

    public static GoldenEyeTestRecord FromSessionRecord(EyeTestSessionRecord record) =>
        new(
            record.ScenarioHash,
            record.BuildArtifactSha256,
            record.Display,
            record.Frames
                .OrderBy(f => f.Step)
                .Select(f => new GoldenFrameFingerprint(f.Step, f.Sha256, f.PerceptualHash))
                .ToArray());
}
