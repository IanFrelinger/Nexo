namespace Ashlar.Abstractions.Barriers.Identity;

/// <summary>
/// Outcome of a barrier identity resolution attempt.
/// Returned by <see cref="IBarrierIdentityResolver"/> when a resolver can map
/// incoming identity material to a concrete barrier level.
/// </summary>
/// <param name="ResolvedLevel">Barrier level chosen by the resolver (e.g. <c>public</c>, <c>internal</c>).</param>
/// <param name="ResolverName">Human-readable resolver that produced this result; used in audit trails.</param>
/// <param name="AuthoritySource">Origin of the authority claim (JWT, mTLS cert, API key, explicit header, etc.).</param>
/// <param name="Detail">Optional diagnostic detail for operators and audit sinks.</param>
public sealed record BarrierResolutionResult(
    string ResolvedLevel,
    string ResolverName,
    string AuthoritySource,
    string? Detail = null);
