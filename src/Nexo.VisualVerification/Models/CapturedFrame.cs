namespace Nexo.VisualVerification;

/// <summary>One captured frame, content-addressed.</summary>
public sealed record CapturedFrame(int Step, string Sha256, string PerceptualHash, string StoragePath);
