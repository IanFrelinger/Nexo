namespace Nexo.Runtime.Barriers.Identity.Resolvers;

internal sealed class PkiCertificateResolverOptions
{
    public IList<CertificateBarrierRule> Rules { get; init; } = [];
}

internal sealed class CertificateBarrierRule
{
    public string Name { get; init; } = string.Empty;
    public string MatchField { get; init; } = string.Empty;
    public string MatchPattern { get; init; } = string.Empty;
    public string BarrierLevel { get; init; } = string.Empty;
}
