namespace Nexo.Core.Application.Environments;

/// <summary>Batch of suggested materials (e.g. facades, roads, water).</summary>
/// <param name="Materials">Suggested material specifications for the engine.</param>
/// <param name="Summary">Optional human-readable summary of the suggestions.</param>
public sealed record MaterialIntelligenceResult(IReadOnlyList<SuggestedMaterialSpec> Materials, string? Summary);
