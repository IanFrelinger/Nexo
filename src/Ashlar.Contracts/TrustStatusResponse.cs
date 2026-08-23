namespace Ashlar.Contracts;

/// <summary>Trust subsystem status for operator and API consumers.</summary>
/// <param name="IsPaused">When true, trust sanitization is temporarily paused.</param>
/// <param name="Status">Human-readable trust subsystem status.</param>
/// <param name="ActivePolicyPackId">Active policy pack id.</param>
/// <param name="ActivePolicyPackVersion">Active policy pack version.</param>
public sealed record TrustStatusResponse(bool IsPaused, string Status, string? ActivePolicyPackId, string? ActivePolicyPackVersion);
