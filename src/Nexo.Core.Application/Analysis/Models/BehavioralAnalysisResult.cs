namespace Nexo.Core.Application.Analysis.Models;

/// <summary>
/// Result of behavioral analysis: comparison of actual output vs declared output contract.
/// </summary>
public record BehavioralAnalysisResult
{
    /// <summary>Whether the brick output satisfies its declared contract.</summary>
    public required bool ContractSatisfied { get; init; }

    /// <summary>Descriptions of contract drift when output diverges from expected.</summary>
    public required IReadOnlyList<string> DriftDescriptions { get; init; }
}
