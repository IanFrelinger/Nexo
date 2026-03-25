namespace Nexo.Runtime.Barriers.Identity.Resolvers;

public sealed class PkiCertificateResolverOptions
{
    public IList<CertificateBarrierRule> Rules { get; init; } = [];
}

public sealed class CertificateBarrierRule
{
    public string Name { get; init; } = string.Empty;
    public string MatchField { get; init; } = string.Empty;
    public string MatchPattern { get; init; } = string.Empty;
    public string BarrierLevel { get; init; } = string.Empty;
}
