namespace Nexo.Runtime.Barriers.Identity;

public sealed class BarrierIdentityResolverOptions
{
    /// <summary>
    /// Resolver registration priority. Lower index = higher priority.
    /// Valid values: PkiCertificate, JwtClaim, ApiKey.
    /// </summary>
    public IList<string> ResolverPriority { get; init; } = [];
}
