namespace Nexo.Abstractions.Barriers.Identity;

public interface IBarrierIdentityResolver
{
    /// <summary>
    /// Attempt to resolve a barrier level from the current identity context.
    /// Returns null if this resolver cannot determine a level from available identity material.
    /// Implementations must not throw and should log internal failures.
    /// </summary>
    ValueTask<BarrierResolutionResult?> TryResolveAsync(
        BarrierResolutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Human-readable resolver name for audit and diagnostics.
    /// </summary>
    string ResolverName { get; }
}
