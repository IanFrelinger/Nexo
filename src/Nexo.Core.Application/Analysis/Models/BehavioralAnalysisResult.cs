namespace Nexo.Core.Application.Analysis.Models;

/// <summary>
/// Result of behavioral analysis: comparison of actual output vs declared output contract.
/// </summary>
public record BehavioralAnalysisResult
{
    public required bool ContractSatisfied { get; init; }
    public required IReadOnlyList<string> DriftDescriptions { get; init; }
}
