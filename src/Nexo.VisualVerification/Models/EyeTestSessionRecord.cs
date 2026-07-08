namespace Nexo.VisualVerification;

/// <summary>
/// Certified output artifact of a visual verification session.
/// <see cref="RecordSha256"/> and <see cref="Ed25519Signature"/> bind all prior fields.
/// </summary>
public sealed record EyeTestSessionRecord(
    string ScenarioHash,
    string BuildArtifactSha256,
    DisplayConfig Display,
    IReadOnlyList<CapturedFrame> Frames,
    VerificationVerdict Verdict,
    string RecordSha256,
    byte[] Ed25519Signature);
