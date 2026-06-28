using Nexo.Certification.State;
using Nexo.Tests.Infrastructure.Certification.Reuse;

namespace Nexo.Tests.Infrastructure.Certification.Reuse;

/// <summary>
/// Project C consumer surface: bundled artifact verification only (no gate, no Infrastructure in Project C exe).
/// </summary>
public static class ProjectCStateLogConsumer
{
    public static StateLogTrustResult VerifyBundledArtifacts(string artifactRoot, string? hmacKey = null) =>
        AttestedStateLogArtifactVerifier.VerifyBundledArtifacts(artifactRoot, hmacKey);

    public static StateLogTrustResult VerifyBundledArtifactsWithLiveSidecar(string artifactRoot, string? hmacKey = null) =>
        AttestedStateLogArtifactVerifier.VerifyBundledArtifacts(
            artifactRoot,
            hmacKey,
            bindLiveStateSidecar: true);

    public static void AssertProjectCProjectHasNoGateOrInfrastructureReferences(string projectCcsprojPath)
    {
        var text = File.ReadAllText(projectCcsprojPath);
        if (text.Contains("Nexo.Infrastructure", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Project C must not reference Nexo.Infrastructure.");
        if (text.Contains("Adaptation.Generation", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Project C must not reference the generator.");
    }
}
