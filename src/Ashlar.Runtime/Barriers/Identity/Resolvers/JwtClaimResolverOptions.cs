namespace Ashlar.Runtime.Barriers.Identity.Resolvers;

/// <summary>Configuration for JWT claim to barrier level mappings.</summary>
public sealed class JwtClaimResolverOptions
{
    /// <summary>
    /// JWT claim name to inspect. Example: ashlar_barrier, tier, clearance.
    /// </summary>
    public string ClaimName { get; init; } = "ashlar_barrier";

    /// <summary>
    /// Maps JWT claim values to barrier level names.
    /// </summary>
    public IDictionary<string, string> ClaimValueMapping { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
