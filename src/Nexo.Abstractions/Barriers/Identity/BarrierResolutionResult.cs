namespace Nexo.Abstractions.Barriers.Identity;

public sealed record BarrierResolutionResult(
    string ResolvedLevel,
    string ResolverName,
    string AuthoritySource,
    string? Detail = null);
