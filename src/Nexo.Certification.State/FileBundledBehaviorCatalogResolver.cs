using System.Text.Json;
using Nexo.Certification.Contracts;

namespace Nexo.Certification.State;

/// <summary>
/// Portable file-backed behavior catalog for bundled artifact reuse (Project C).
/// Each behavior lives in <c>{contentHash}/record.json</c> + <c>{contentHash}/source.txt</c>.
/// </summary>
public sealed class FileBundledBehaviorCatalogResolver : ICertificateResolver
{
    private readonly string _directory;

    public FileBundledBehaviorCatalogResolver(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Behavior catalog directory is required.", nameof(directory));

        _directory = directory;
    }

    public CertificateResolveResult Resolve(string behaviorCertContentHash)
    {
        if (string.IsNullOrWhiteSpace(behaviorCertContentHash))
            return new CertificateResolveResult(false, null, null);

        foreach (var behaviorDir in BundledBehaviorPaths.CandidateDirectories(_directory, behaviorCertContentHash))
        {
            var recordPath = Path.Combine(behaviorDir, "record.json");
            var sourcePath = Path.Combine(behaviorDir, "source.txt");

            if (!File.Exists(recordPath) || !File.Exists(sourcePath))
                continue;

            var record = JsonSerializer.Deserialize<CertificationRecordData>(
                File.ReadAllText(recordPath),
                JsonOptions);

            if (record is null || string.IsNullOrWhiteSpace(record.ContentHash))
                continue;

            if (!string.Equals(record.ContentHash, behaviorCertContentHash, StringComparison.Ordinal))
                continue;

            return new CertificateResolveResult(true, record, File.ReadAllText(sourcePath));
        }

        return new CertificateResolveResult(false, null, null);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
