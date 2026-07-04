namespace Nexo.Certification.Physical;

public sealed record PhysicalAtomTrustResult(bool Trusted, string? FailureCode, string? Reason);
