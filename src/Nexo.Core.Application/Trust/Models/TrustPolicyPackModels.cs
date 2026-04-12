namespace Nexo.Core.Application.Trust.Models;

/// <summary>
/// Describes a versioned trust policy pack that can be applied in one step.
/// </summary>
public sealed record TrustPolicyPack(
    string Id,
    string Version,
    string DisplayName,
    string Description,
    bool PauseObservationByDefault,
    IReadOnlyDictionary<string, bool> CategoryRules,
    IReadOnlyDictionary<string, bool> SourceRules,
    IReadOnlyList<TrustPolicyPackProjectRule> ProjectRules,
    string? RecommendedFor = null);

/// <summary>
/// Per-project source overrides for a trust policy pack.
/// </summary>
public sealed record TrustPolicyPackProjectRule(
    string ProjectPath,
    IReadOnlyDictionary<string, bool> SourceRules);

/// <summary>
/// Lightweight list item for CLI/API status surfaces.
/// </summary>
public sealed record TrustPolicyPackInfo(
    string Id,
    string Version,
    string DisplayName,
    string Description,
    string? RecommendedFor = null);

/// <summary>
/// Tracks the currently active trust policy pack.
/// </summary>
public sealed record TrustPolicyPackStatus(
    string? ActivePackId = null,
    string? ActivePackVersion = null,
    DateTimeOffset? ActivatedAtUtc = null);

/// <summary>
/// Runtime view of the currently active policy pack.
/// </summary>
public sealed record ActiveTrustPolicyPack(
    string Id,
    string Version,
    DateTimeOffset ActivatedAtUtc);
