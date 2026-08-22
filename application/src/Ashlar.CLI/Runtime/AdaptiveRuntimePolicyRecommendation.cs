namespace Ashlar.CLI.Runtime;

/// <summary>Recommended QA policy profile resolved from adaptive runtime signals.</summary>
public sealed record AdaptiveRuntimePolicyRecommendation(string QaPolicy, string Reason);
