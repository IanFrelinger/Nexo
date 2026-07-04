namespace Nexo.Certification.Contracts;

/// <summary>Outcome of external certification trust verification.</summary>
/// <param name="Trusted">True when the record and brick source pass all checks.</param>
/// <param name="FailureCode">Canonical failure code when <paramref name="Trusted"/> is false.</param>
/// <param name="Reason">Human-readable failure explanation.</param>
public sealed record CertificationTrustResult(bool Trusted, string? FailureCode, string? Reason);
