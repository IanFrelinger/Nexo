namespace Ashlar.Abstractions.Barriers.Identity;

/// <summary>
/// Ordered pipeline of <see cref="IBarrierIdentityResolver"/> implementations.
/// Runs resolvers in priority order until one produces a non-null result.
/// </summary>
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
