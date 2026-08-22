namespace Ashlar.Abstractions.Barriers.Identity;

/// <summary>
/// Identity material available to barrier resolvers for a single inbound request.
/// Aggregates explicit overrides, transport headers, certificate subjects, JWT claims,
/// and API-key hints so resolvers can derive a barrier level without re-parsing wire data.
/// </summary>
/// <param name="CorrelationId">Request correlation id for audit and tracing.</param>
/// <param name="ExplicitLevel">Caller-supplied barrier level override, when present.</param>
/// <param name="Headers">Normalized inbound HTTP or transport headers.</param>
/// <param name="CertSubjects">Client certificate subject distinguished names, if mTLS is in use.</param>
/// <param name="CertSans">Client certificate subject alternative names, if mTLS is in use.</param>
/// <param name="RawJwt">Raw bearer JWT token, when supplied by the caller.</param>
/// <param name="JwtClaims">Pre-parsed JWT claims; may be populated by <see cref="JwtClaimParser"/>.</param>
/// <param name="ApiKey">API key value extracted from headers or query, when present.</param>
public sealed record BarrierResolutionContext(
    string CorrelationId,
    string? ExplicitLevel,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyList<string> CertSubjects,
    IReadOnlyList<string> CertSans,
    string? RawJwt,
    IReadOnlyDictionary<string, string> JwtClaims,
    string? ApiKey);
