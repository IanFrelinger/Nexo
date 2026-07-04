namespace Nexo.Core.Application.Trust.Models;

/// <summary>
/// Per-project source overrides for a trust policy pack.
/// </summary>
/// <param name="ProjectPath">Absolute or workspace-relative project path.</param>
/// <param name="SourceRules">Source allow/deny overrides for this project (true = allowed).</param>
public sealed record TrustPolicyPackProjectRule(
    string ProjectPath,
    IReadOnlyDictionary<string, bool> SourceRules);
