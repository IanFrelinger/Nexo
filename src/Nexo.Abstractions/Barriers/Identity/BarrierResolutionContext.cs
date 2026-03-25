namespace Nexo.Abstractions.Barriers.Identity;

public sealed record BarrierResolutionContext(
    string CorrelationId,
    string? ExplicitLevel,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyList<string> CertSubjects,
    IReadOnlyList<string> CertSans,
    string? RawJwt,
    IReadOnlyDictionary<string, string> JwtClaims,
    string? ApiKey);
