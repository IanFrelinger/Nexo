namespace Nexo.Core.Application.Trust.Models;

/// <summary>
/// Describes a versioned trust policy pack that can be applied in one step.
/// </summary>
/// <param name="Id">Stable pack identifier.</param>
/// <param name="Version">Semantic version of the pack.</param>
/// <param name="DisplayName">Human-readable name for CLI and UI.</param>
/// <param name="Description">Summary of what the pack configures.</param>
/// <param name="PauseObservationByDefault">Whether observation starts paused when the pack is applied.</param>
/// <param name="CategoryRules">Category allow/deny rules (true = allowed).</param>
/// <param name="SourceRules">Global source allow/deny rules (true = allowed).</param>
/// <param name="ProjectRules">Per-project source override rules.</param>
/// <param name="RecommendedFor">Optional audience hint (e.g. enterprise, developer).</param>
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
