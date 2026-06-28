using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// In-memory certified behavior catalog keyed by content hash.
/// </summary>
public sealed class InMemoryCertifiedBehaviorCatalog : ICertifiedBehaviorCatalog
{
    private readonly Dictionary<string, CertifiedBehaviorEntry> _entries;

    public InMemoryCertifiedBehaviorCatalog(IEnumerable<CertifiedBehaviorEntry> entries)
    {
        _entries = entries.ToDictionary(entry => entry.ContentHash, StringComparer.Ordinal);
    }

    public CertifiedBehaviorCatalogLookup Lookup(string behaviorCertContentHash)
    {
        if (_entries.TryGetValue(behaviorCertContentHash, out var entry))
            return CertifiedBehaviorCatalogLookup.Hit(entry);

        return CertifiedBehaviorCatalogLookup.Miss();
    }
}
