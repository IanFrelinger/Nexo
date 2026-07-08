namespace Nexo.VisualVerification;

/// <summary>Pixel-tier verification outcome for an eye-test session.</summary>
public enum VerdictTier
{
    ExactMatch,
    PerceptualMatch,
    Mismatch,
    NonDeterministic,
    CaptureFailure
}
