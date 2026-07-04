namespace Nexo.Runtime.Barriers.Identity;

/// <summary>Configuration for barrier identity resolver registration order.</summary>
public sealed class BarrierIdentityResolverOptions
{
    /// <summary>
    /// Resolver registration priority. Lower index = higher priority.
    /// Valid values: PkiCertificate, JwtClaim, ApiKey.
    /// </summary>
    public IList<string> ResolverPriority { get; init; } = [];
}
