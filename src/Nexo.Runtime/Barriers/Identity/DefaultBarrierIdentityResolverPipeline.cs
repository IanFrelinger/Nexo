using Microsoft.Extensions.Logging;
using Nexo.Abstractions.Barriers;
using Nexo.Abstractions.Barriers.Identity;

namespace Nexo.Runtime.Barriers.Identity;

public sealed class DefaultBarrierIdentityResolverPipeline : IBarrierIdentityResolverPipeline
{
    private const string BarrierHeader = "x-nexo-barrier";

    private readonly IReadOnlyList<IBarrierIdentityResolver> _resolvers;
    private readonly ILogger<DefaultBarrierIdentityResolverPipeline> _logger;

    public DefaultBarrierIdentityResolverPipeline(
        IEnumerable<IBarrierIdentityResolver> resolvers,
        ILogger<DefaultBarrierIdentityResolverPipeline> logger)
    {
        _resolvers = (resolvers ?? Array.Empty<IBarrierIdentityResolver>()).ToList();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<BarrierResolutionResult?> ResolveAsync(
        BarrierResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (!string.IsNullOrWhiteSpace(context.ExplicitLevel))
        {
            var explicitLevel = context.ExplicitLevel!;
            var authority = context.Headers.ContainsKey(BarrierHeader)
                ? BarrierAuthoritySource.Header
                : BarrierAuthoritySource.Cli;
            var explicitResult = new BarrierResolutionResult(
                explicitLevel,
                ResolverName: "Explicit",
                AuthoritySource: authority,
                Detail: "Explicit barrier provided at request boundary.");
            _logger.LogDebug(
                "Barrier identity resolution matched explicit level. Resolver={ResolverName} Level={Level} CorrelationId={CorrelationId}",
                explicitResult.ResolverName,
                explicitResult.ResolvedLevel,
                context.CorrelationId);
            return explicitResult;
        }

        foreach (var resolver in _resolvers)
        {
            try
            {
                var result = await resolver.TryResolveAsync(context, cancellationToken);
                if (result is null)
                {
                    _logger.LogDebug(
                        "Barrier identity resolver had no match. Resolver={ResolverName} CorrelationId={CorrelationId}",
                        resolver.ResolverName,
                        context.CorrelationId);
                    continue;
                }

                _logger.LogDebug(
                    "Barrier identity resolved. Resolver={ResolverName} Level={Level} CorrelationId={CorrelationId}",
                    result.ResolverName,
                    result.ResolvedLevel,
                    context.CorrelationId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Barrier identity resolver failed. Resolver={ResolverName} CorrelationId={CorrelationId}",
                    resolver.ResolverName,
                    context.CorrelationId);
            }
        }

        _logger.LogDebug(
            "Barrier identity resolution returned no match. CorrelationId={CorrelationId}",
            context.CorrelationId);
        return null;
    }
}
