using Nexo.Certification.State;
using Nexo.Core.Application.Certification.Ports;

namespace Nexo.Infrastructure.Certification;

/// <summary>
/// Adapts <see cref="ICertifiedBehaviorCatalog"/> to <see cref="ICertificateResolver"/>.
/// </summary>
public sealed class CertifiedBehaviorCertificateResolver : ICertificateResolver
{
    private readonly ICertifiedBehaviorCatalog _catalog;

    public CertifiedBehaviorCertificateResolver(ICertifiedBehaviorCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public CertificateResolveResult Resolve(string behaviorCertContentHash)
    {
        var lookup = _catalog.Lookup(behaviorCertContentHash);
        if (!lookup.Found || lookup.Entry is null)
            return new CertificateResolveResult(false, null, null);

        return new CertificateResolveResult(true, lookup.Entry.Record, lookup.Entry.Source);
    }
}
