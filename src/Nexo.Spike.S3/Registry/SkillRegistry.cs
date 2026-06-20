using System.Text.Json;
using System.Text.Json.Serialization;
using Nexo.Spike.S3.Matching;
using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Registry;

public sealed record LookupResult(bool Found, RegistryEntry? Entry);

/// <summary>
/// File-backed skill catalog under <c>artifacts/s3/registry/</c>.
/// Admission is the only write path.
/// </summary>
public sealed class SkillRegistry
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _registryRoot;

    public SkillRegistry(string? registryRoot = null)
    {
        _registryRoot = registryRoot ?? RegistryPaths.ResolveDefaultRegistryRoot();
    }

    public string RegistryRoot => _registryRoot;

    public LookupResult Lookup(IntentDescriptor intent)
    {
        foreach (var entry in LoadAllEntries())
        {
            if (IntentMatcher.Matches(intent, entry.Intent))
                return new LookupResult(true, entry);
        }

        return new LookupResult(false, null);
    }

    public IReadOnlyList<RegistryEntry> ListEntries() => LoadAllEntries();

    public AdmissionResult Admit(SkillCandidate candidate, SignedCertificationRecord certification)
    {
        var evaluation = PromotionContractEvaluator.Evaluate(certification);
        if (!evaluation.Satisfied)
        {
            return new AdmissionResult(
                AdmissionStatus.Rejected,
                null,
                string.Join("; ", evaluation.Violations));
        }

        var entryId = IntentMatcher.ComputeLookupKey(candidate.Intent);
        var contentHash = RegistryPaths.ComputeContentHash(candidate.ImplementationSource, candidate.Intent);
        var entry = new RegistryEntry(
            entryId,
            candidate.Intent,
            candidate.CandidateId,
            contentHash,
            candidate.ImplementationSource,
            candidate.IntentSpecPath,
            certification,
            DateTimeOffset.UtcNow);

        WriteEntry(entry);
        UpdateCatalog(entry);
        return new AdmissionResult(AdmissionStatus.Admitted, entry, null);
    }

    internal void WriteEntryWithoutContractCheck(RegistryEntry entry)
    {
        WriteEntry(entry);
        UpdateCatalog(entry);
    }

    private void WriteEntry(RegistryEntry entry)
    {
        var entryDir = RegistryPaths.EntryDirectory(_registryRoot, entry.EntryId);
        Directory.CreateDirectory(entryDir);

        File.WriteAllText(
            RegistryPaths.ImplementationFilePath(_registryRoot, entry.EntryId),
            entry.ImplementationSource);

        File.WriteAllText(
            RegistryPaths.EntryFilePath(_registryRoot, entry.EntryId),
            JsonSerializer.Serialize(entry, SerializerOptions));
    }

    private void UpdateCatalog(RegistryEntry entry)
    {
        var catalogPath = RegistryPaths.CatalogPath(_registryRoot);
        Directory.CreateDirectory(_registryRoot);

        var catalog = File.Exists(catalogPath)
            ? JsonSerializer.Deserialize<RegistryCatalog>(File.ReadAllText(catalogPath), SerializerOptions)
              ?? new RegistryCatalog()
            : new RegistryCatalog();

        var ids = catalog.EntryIds.ToList();
        if (!ids.Contains(entry.EntryId, StringComparer.Ordinal))
            ids.Add(entry.EntryId);

        catalog = catalog with
        {
            EntryIds = ids.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        File.WriteAllText(catalogPath, JsonSerializer.Serialize(catalog, SerializerOptions));
    }

    private List<RegistryEntry> LoadAllEntries()
    {
        var catalogPath = RegistryPaths.CatalogPath(_registryRoot);
        if (!File.Exists(catalogPath))
            return [];

        var catalog = JsonSerializer.Deserialize<RegistryCatalog>(File.ReadAllText(catalogPath), SerializerOptions);
        if (catalog?.EntryIds is null || catalog.EntryIds.Count == 0)
            return [];

        var entries = new List<RegistryEntry>();
        foreach (var entryId in catalog.EntryIds)
        {
            var entryPath = RegistryPaths.EntryFilePath(_registryRoot, entryId);
            if (!File.Exists(entryPath))
                continue;

            var entry = JsonSerializer.Deserialize<RegistryEntry>(File.ReadAllText(entryPath), SerializerOptions);
            if (entry is not null)
                entries.Add(entry);
        }

        return entries;
    }

    private sealed record RegistryCatalog
    {
        public IReadOnlyList<string> EntryIds { get; init; } = [];
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
