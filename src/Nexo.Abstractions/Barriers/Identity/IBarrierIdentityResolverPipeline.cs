namespace Nexo.Abstractions.Barriers.Identity;

public interface IBarrierIdentityResolverPipeline
{
    /// <summary>
    /// Run registered resolvers in priority order.
    /// Returns the first non-null result or null if no resolver matched.
    /// </summary>
    ValueTask<BarrierResolutionResult?> ResolveAsync(
        BarrierResolutionContext context,
        CancellationToken cancellationToken = default);
}
