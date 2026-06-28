using Nexo.Certification.State;
using CertifiedStateLogReuse.ProjectC;

return await ProgramEntry.RunAsync(args);

namespace CertifiedStateLogReuse.ProjectC;

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        var artifactRoot = args.ElementAtOrDefault(0)
            ?? throw new InvalidOperationException(
                "Usage: ProjectC <path-to-Nexo.Certified.PhaseWitness> [hmac-key]");

        var hmacKey = args.ElementAtOrDefault(1) ?? "attested-state-log-test-hmac";
        var trust = ProjectCStateLogConsumer.VerifyBundledArtifacts(artifactRoot, hmacKey);

        if (!trust.Trusted)
        {
            await Console.Error.WriteLineAsync($"UNTRUSTED: {trust.FailureCode} — {trust.Reason}").ConfigureAwait(false);
            return 2;
        }

        await Console.Out.WriteLineAsync($"TRUSTED verifiedTransitions={trust.VerifiedTransitions}").ConfigureAwait(false);
        return 0;
    }
}

internal static class ProjectCStateLogConsumer
{
    public static StateLogTrustResult VerifyBundledArtifacts(string artifactRoot, string? hmacKey = null)
    {
        var schemaPath = Path.Combine(artifactRoot, "state-schema.json");
        var logPath = Path.Combine(artifactRoot, "attested-state-log.json");
        var behaviorRoot = Path.Combine(artifactRoot, "behaviors");

        var schema = AttestedStateLogWireFormat.DeserializeSchema(File.ReadAllText(schemaPath));
        var log = AttestedStateLogWireFormat.DeserializeLog(File.ReadAllText(logPath));
        var catalog = new FileBundledBehaviorCatalog(behaviorRoot);
        var resolver = new CatalogCertificateResolver(catalog);

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

internal sealed class FileBundledBehaviorCatalog
{
    private readonly string _directory;

    public FileBundledBehaviorCatalog(string directory) => _directory = directory;

    public bool TryGet(string contentHash, out Nexo.Certification.Contracts.CertificationRecordData record, out string source)
    {
        record = null!;
        source = null!;

        var behaviorDir = Path.Combine(_directory, contentHash);
        var recordPath = Path.Combine(behaviorDir, "record.json");
        var sourcePath = Path.Combine(behaviorDir, "source.txt");
        if (!File.Exists(recordPath) || !File.Exists(sourcePath))
            return false;

        record = System.Text.Json.JsonSerializer.Deserialize<Nexo.Certification.Contracts.CertificationRecordData>(
            File.ReadAllText(recordPath),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Invalid certification record JSON.");

        source = File.ReadAllText(sourcePath);
        return true;
    }
}

internal sealed class CatalogCertificateResolver : ICertificateResolver
{
    private readonly FileBundledBehaviorCatalog _catalog;

    public CatalogCertificateResolver(FileBundledBehaviorCatalog catalog) => _catalog = catalog;

    public CertificateResolveResult Resolve(string behaviorCertContentHash)
    {
        if (_catalog.TryGet(behaviorCertContentHash, out var record, out var source))
            return new CertificateResolveResult(true, record, source);

        return new CertificateResolveResult(false, null, null);
    }
}
