using Nexo.Core.Application.Analysis.Models;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Analysis.BrickAnalyzer;

/// <summary>
/// Compares actual brick output to declared BrickInterface output contract.
/// </summary>
public sealed class BehavioralAnalyzer : IBehavioralAnalyzer
{
    /// <inheritdoc />
    public Task<BehavioralAnalysisResult> AnalyzeAsync(
        DomainBrick brick,
        BrickOutput actualOutput,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var drift = new List<string>();
        var expectedOutputs = brick.Interface?.Outputs ?? new List<BrickOutputDefinition>();
        var actualKeys = actualOutput.ToDictionary().Keys.ToHashSet(StringComparer.Ordinal);

        foreach (var output in expectedOutputs)
        {
            if (!actualKeys.Contains(output.Name))
            {
                drift.Add($"Missing declared output '{output.Name}' (expected type: {output.Type})");
            }
        }

        foreach (var key in actualKeys)
        {
            if (!expectedOutputs.Any(o => o.Name.Equals(key, StringComparison.Ordinal)))
            {
                drift.Add($"Unexpected output '{key}' not declared in BrickInterface");
            }
        }

        return Task.FromResult(new BehavioralAnalysisResult
        {
            ContractSatisfied = drift.Count == 0,
            DriftDescriptions = drift,
        });
    }
}
