using Nexo.Core.Application.Certification.Models;

namespace Nexo.Core.Application.Certification.Ports;

/// <summary>
/// Content-hash keyed catalog of certified behaviors (record + source) for state log verification.
/// </summary>
public interface ICertifiedBehaviorCatalog
{
    CertifiedBehaviorCatalogLookup Lookup(string behaviorCertContentHash);
}

/// <summary>
/// Result of looking up a certified behavior by content hash.
/// </summary>
public sealed class CertifiedBehaviorCatalogLookup
{
    public bool Found { get; init; }
    public CertifiedBehaviorEntry? Entry { get; init; }

    public static CertifiedBehaviorCatalogLookup Miss() => new() { Found = false };

    public static CertifiedBehaviorCatalogLookup Hit(CertifiedBehaviorEntry entry) =>
        new() { Found = true, Entry = entry };
}
