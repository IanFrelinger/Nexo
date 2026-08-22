using Microsoft.Extensions.Logging;
using Ashlar.Abstractions.Barriers;
using Ashlar.Abstractions.Barriers.Identity;

namespace Ashlar.Runtime.Barriers.Identity.Resolvers;

/// <summary>
/// Resolves barrier level from pre-validated JWT claims.
/// This resolver does not validate JWT signatures; host middleware must do that first.
/// </summary>
public sealed class JwtClaimBarrierResolver : IBarrierIdentityResolver
{
    private readonly JwtClaimResolverOptions _options;
    private readonly BarrierHierarchy _hierarchy;
    private readonly ILogger<JwtClaimBarrierResolver> _logger;

    public JwtClaimBarrierResolver(
        JwtClaimResolverOptions options,
        BarrierHierarchy hierarchy,
        ILogger<JwtClaimBarrierResolver> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Resolver identifier used in audit and diagnostics.</summary>
    public string ResolverName => "JwtClaim";

    /// <summary>Attempts to resolve barrier level from pre-validated JWT claim values.</summary>
    public ValueTask<BarrierResolutionResult?> TryResolveAsync(
        BarrierResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (context is null || string.IsNullOrWhiteSpace(context.RawJwt))
            {
                return default;
            }

            var claimName = _options.ClaimName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(claimName))
            {
                return default;
            }

            if (!context.JwtClaims.TryGetValue(claimName, out var claimValue))
            {
                return default;
            }

            if (!_options.ClaimValueMapping.TryGetValue(claimValue, out var mappedLevel))
            {
                return default;
            }

            if (!_hierarchy.IsKnown(mappedLevel))
            {
                _logger.LogWarning(
                    "JWT claim mapped to unknown barrier level. ClaimName={ClaimName} ClaimValue={ClaimValue} BarrierLevel={BarrierLevel}",
                    claimName,
                    claimValue,
                    mappedLevel);
                return default;
            }

            var result = new BarrierResolutionResult(
                ResolvedLevel: mappedLevel,
                ResolverName: ResolverName,
                AuthoritySource: BarrierAuthoritySource.JwtClaim,
                Detail: $"Claim '{claimName}={claimValue}' mapped to '{mappedLevel}'.");
            return new ValueTask<BarrierResolutionResult?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JWT barrier resolution failed.");
            return default;
        }
    }
}
