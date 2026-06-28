namespace Nexo.Tests.Infrastructure.Certification.Reuse;

/// <summary>
/// Shared paths for bundled phase-witness attested state log artifacts (Project C).
/// </summary>
public static class PhaseWitnessPaths
{
    public static string ArtifactRoot() =>
        Path.Combine(
            RepoPaths.FindRepoRoot(),
            "samples", "certified-state-log-reuse", "Nexo.Certified.PhaseWitness");

    public static string SchemaPath(string artifactRoot) => Path.Combine(artifactRoot, "state-schema.json");

    public static string LogPath(string artifactRoot) => Path.Combine(artifactRoot, "attested-state-log.json");

    public static string BehaviorRoot(string artifactRoot) => Path.Combine(artifactRoot, "behaviors");
}
