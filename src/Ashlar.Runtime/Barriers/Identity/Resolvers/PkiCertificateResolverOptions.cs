namespace Ashlar.Runtime.Barriers.Identity.Resolvers;

/// <summary>Configuration for PKI certificate barrier resolution rules.</summary>
public sealed class PkiCertificateResolverOptions
{
    /// <summary>Ordered rules mapping certificate fields to barrier levels.</summary>
    public IList<CertificateBarrierRule> Rules { get; init; } = [];
}
