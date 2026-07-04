namespace Nexo.Abstractions.Barriers.Identity;

/// <summary>
/// Maps inbound identity material to a concrete barrier level.
/// Each resolver inspects a specific authority source (JWT, mTLS, API key, etc.)
/// and returns <see langword="null"/> when it cannot resolve from available context.
/// </summary>
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
