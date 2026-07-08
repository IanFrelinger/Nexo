namespace Nexo.VisualVerification;

/// <summary>Identifies the target application build. The harness never inspects internals.</summary>
public sealed record BuildArtifactRef(string Path, string Sha256);
