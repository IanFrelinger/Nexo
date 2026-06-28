using System.Text.Json;
using Nexo.Certification.Contracts;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// File-backed certified behavior catalog. Each behavior lives in
/// <c>{contentHash}/record.json</c> + <c>{contentHash}/source.txt</c>.
/// </summary>
public sealed class FileCertifiedBehaviorCatalog : ICertifiedBehaviorCatalog
{
    private readonly string _directory;

    public FileCertifiedBehaviorCatalog(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Catalog directory is required.", nameof(directory));

        _directory = directory;
    }

    public CertifiedBehaviorCatalogLookup Lookup(string behaviorCertContentHash)
    {
        if (string.IsNullOrWhiteSpace(behaviorCertContentHash))
            return CertifiedBehaviorCatalogLookup.Miss();

        var behaviorDir = Path.Combine(_directory, behaviorCertContentHash);
        var recordPath = Path.Combine(behaviorDir, "record.json");
        var sourcePath = Path.Combine(behaviorDir, "source.txt");

        if (!File.Exists(recordPath) || !File.Exists(sourcePath))
            return CertifiedBehaviorCatalogLookup.Miss();

        var record = JsonSerializer.Deserialize<CertificationRecordData>(
            File.ReadAllText(recordPath),
            JsonOptions);

        if (record is null || string.IsNullOrWhiteSpace(record.ContentHash))
            return CertifiedBehaviorCatalogLookup.Miss();

        if (!string.Equals(record.ContentHash, behaviorCertContentHash, StringComparison.Ordinal))
            return CertifiedBehaviorCatalogLookup.Miss();

        var source = File.ReadAllText(sourcePath);
        return CertifiedBehaviorCatalogLookup.Hit(new CertifiedBehaviorEntry
        {
            ContentHash = behaviorCertContentHash,
            Record = record,
            Source = source
        });
    }

    public static void WriteEntry(string directory, CertifiedBehaviorEntry entry)
    {
        var behaviorDir = Path.Combine(directory, entry.ContentHash);
        Directory.CreateDirectory(behaviorDir);
        File.WriteAllText(
            Path.Combine(behaviorDir, "record.json"),
            JsonSerializer.Serialize(entry.Record, JsonOptions));
        File.WriteAllText(Path.Combine(behaviorDir, "source.txt"), entry.Source);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
