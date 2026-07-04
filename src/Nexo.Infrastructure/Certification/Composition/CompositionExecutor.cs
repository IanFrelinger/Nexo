using System.Text.Json;
using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Executes a declared composition graph against certified constituent bricks.
/// </summary>
internal sealed class CompositionExecutor
{
    /// <summary>Run asynchronously.</summary>
    public async Task<CompositionRunResult> RunAsync(
        CompositionSpec spec,
        IReadOnlyDictionary<string, object> compositionInput,
        IBrickRegistry brickRegistry,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var nodeOutputs = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
        var executionOrder = CompositionGraphTopology.GetExecutionOrder(spec);

        foreach (var nodeId in executionOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var node = spec.Nodes.First(n => string.Equals(n.NodeId, nodeId, StringComparison.Ordinal));
            var brick = brickRegistry.GetBrick(node.BrickId)
                ?? throw new InvalidOperationException($"Constituent brick '{node.BrickId}' not found in registry");

            var brickInputValues = BuildNodeInput(spec, nodeId, compositionInput, nodeOutputs);
            var output = await brick.ExecuteAsync(
                new BrickInput(brickInputValues),
                ImplementationType.Deterministic,
                context,
                cancellationToken).ConfigureAwait(false);

            nodeOutputs[nodeId] = new Dictionary<string, object>(output.ToDictionary(), StringComparer.OrdinalIgnoreCase);
        }

        var compositionOutput = BuildCompositionOutput(spec, nodeOutputs);
        return new CompositionRunResult(true, compositionOutput, Array.Empty<string>());
    }

    /// <summary>Run witness asynchronously.</summary>
    public async Task<WitnessRunResult> RunWitnessAsync(
        CompositionSpec spec,
        CompositionWitnessSpec witness,
        IBrickRegistry brickRegistry,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();

        foreach (var (caseIndex, witnessCase) in witness.Cases.Select((c, i) => (i, c)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            CompositionRunResult run;
            try
            {
                run = await RunAsync(spec, witnessCase.Input, brickRegistry, context, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                failures.Add($"case {caseIndex}: execution threw {ex.Message}");
                continue;
            }

            foreach (var (key, expected) in witnessCase.ExpectedOutput)
            {
                if (!run.Output.TryGetValue(key, out var actual))
                {
                    failures.Add($"case {caseIndex}: missing output key '{key}'");
                    continue;
                }

                if (!WitnessValueComparer.AreEqual(expected, actual))
                {
                    failures.Add(
                        $"case {caseIndex}: output['{key}'] expected {expected} got {actual}");
                }
            }
        }

        return new WitnessRunResult(failures.Count == 0, failures);
    }

    /// <summary>Runs the composition witness twice and compares outputs for determinism.</summary>
    public async Task<(bool Identical, string? First, string? Second)> CheckDeterminismAsync(
        CompositionSpec spec,
        CompositionWitnessSpec witness,
        IBrickRegistry brickRegistry,
        CancellationToken cancellationToken = default)
    {
        if (witness.Cases.Count == 0)
            return (true, null, null);

        var auditContext = new AuditExecutionContext();
        var probe = witness.Cases[0];

        var first = await RunAsync(spec, probe.Input, brickRegistry, auditContext, cancellationToken)
            .ConfigureAwait(false);
        var second = await RunAsync(spec, probe.Input, brickRegistry, auditContext, cancellationToken)
            .ConfigureAwait(false);

        var firstJson = ToCanonicalJson(first.Output);
        var secondJson = ToCanonicalJson(second.Output);
        return (firstJson == secondJson, firstJson, secondJson);
    }

    private static string ToCanonicalJson(IReadOnlyDictionary<string, object> output)
    {
        var payload = output
            .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
    }

    private static Dictionary<string, object> BuildNodeInput(
        CompositionSpec spec,
        string nodeId,
        IReadOnlyDictionary<string, object> compositionInput,
        IReadOnlyDictionary<string, Dictionary<string, object>> nodeOutputs)
    {
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in spec.Edges.Where(e => string.Equals(e.TargetNodeId, nodeId, StringComparison.Ordinal)))
        {
            object? value = null;
            if (string.Equals(edge.SourceNodeId, CompositionGraphConstants.InputNodeId, StringComparison.Ordinal))
            {
                compositionInput.TryGetValue(edge.SourcePort, out value);
            }
            else if (nodeOutputs.TryGetValue(edge.SourceNodeId, out var outputs))
            {
                outputs.TryGetValue(edge.SourcePort, out value);
            }

            if (value is not null)
                values[edge.TargetPort] = value;
        }

        return values;
    }

    private static Dictionary<string, object> BuildCompositionOutput(
        CompositionSpec spec,
        IReadOnlyDictionary<string, Dictionary<string, object>> nodeOutputs)
    {
        var output = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in spec.Edges.Where(e =>
                     string.Equals(e.TargetNodeId, CompositionGraphConstants.OutputNodeId, StringComparison.Ordinal)))
        {
            if (string.Equals(edge.SourceNodeId, CompositionGraphConstants.InputNodeId, StringComparison.Ordinal))
                continue;

            if (nodeOutputs.TryGetValue(edge.SourceNodeId, out var source)
                && source.TryGetValue(edge.SourcePort, out var value))
            {
                output[edge.TargetPort] = value;
            }
        }

        return output;
    }
}
