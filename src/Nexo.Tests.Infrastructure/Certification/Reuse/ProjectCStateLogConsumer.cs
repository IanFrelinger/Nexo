using Nexo.Certification.State;
using Nexo.Infrastructure.Certification;
using Nexo.Tests.Infrastructure.Certification.Reuse;

namespace Nexo.Tests.Infrastructure.Certification.Reuse;

/// <summary>
/// Project C consumer surface: bundled artifact verification only (no gate, no Infrastructure in Project C exe).
/// </summary>
public static class ProjectCStateLogConsumer
{
    public static StateLogTrustResult VerifyBundledArtifacts(string artifactRoot, string? hmacKey = null)
    {
        var schema = AttestedStateLogWireFormat.DeserializeSchema(
            File.ReadAllText(PhaseWitnessPaths.SchemaPath(artifactRoot)));
        var log = AttestedStateLogWireFormat.DeserializeLog(
            File.ReadAllText(PhaseWitnessPaths.LogPath(artifactRoot)));
        var catalog = new FileCertifiedBehaviorCatalog(PhaseWitnessPaths.BehaviorRoot(artifactRoot));
        var resolver = new CertifiedBehaviorCertificateResolver(catalog);

        return StateLogVerifier.Verify(log, schema, resolver, hmacKey);
    }

    public static void AssertProjectCProjectHasNoGateOrInfrastructureReferences(string projectCcsprojPath)
    {
        var text = File.ReadAllText(projectCcsprojPath);
        if (text.Contains("Nexo.Infrastructure", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Project C must not reference Nexo.Infrastructure.");
        if (text.Contains("Adaptation.Generation", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Project C must not reference the generator.");
    }
}
