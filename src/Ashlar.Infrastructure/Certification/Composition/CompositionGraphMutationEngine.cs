using Ashlar.Core.Application.Certification.Models;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Infrastructure.Certification.Composition;

/// <summary>Generates structural graph mutants and runs composition witness tests.</summary>
internal sealed class CompositionGraphMutationEngine
{
    private readonly CompositionExecutor _executor = new();

    /// <summary>Run asynchronously.</summary>
    public async Task<CompositionMutationTestResult> RunAsync(
        CompositionSpec spec,
        CompositionWitnessSpec witness,
        IBrickRegistry brickRegistry,
        CancellationToken cancellationToken = default)
    {
        var survivors = new List<string>();
        var killed = new List<string>();
        var mutations = CompositionGraphMutationCatalog.CollectMutations(spec, brickRegistry);

        foreach (var mutation in mutations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (StructuralMutationIsNoOp(spec, mutation.MutatedSpec))
            {
                killed.Add(mutation.Id);
                continue;
            }

            var seam = CompositionSeamChecker.Check(mutation.MutatedSpec, brickRegistry);
            if (!seam.Passed)
            {
                killed.Add(mutation.Id);
                continue;
            }

            var witnessPassed = await _executor.RunWitnessAsync(
                mutation.MutatedSpec,
                witness,
                brickRegistry,
                new AuditExecutionContext(),
                cancellationToken).ConfigureAwait(false);

            if (witnessPassed.Passed)
                survivors.Add(mutation.Id);
            else
                killed.Add(mutation.Id);
        }

        var total = survivors.Count + killed.Count;
        var escapeRate = total == 0 ? 0d : (double)survivors.Count / total;
        return new CompositionMutationTestResult(total, survivors, killed, escapeRate);
    }

    private static bool StructuralMutationIsNoOp(CompositionSpec original, CompositionSpec mutant)
    {
        if (original.Nodes.Count != mutant.Nodes.Count)
            return false;

        for (var i = 0; i < original.Nodes.Count; i++)
        {
            if (!string.Equals(original.Nodes[i].NodeId, mutant.Nodes[i].NodeId, StringComparison.Ordinal)
                || !string.Equals(original.Nodes[i].BrickId, mutant.Nodes[i].BrickId, StringComparison.Ordinal))
                return false;
        }

        if (original.Edges.Count != mutant.Edges.Count)
            return false;

        for (var i = 0; i < original.Edges.Count; i++)
        {
            var a = original.Edges[i];
            var b = mutant.Edges[i];
            if (!string.Equals(a.SourceNodeId, b.SourceNodeId, StringComparison.Ordinal)
                || !string.Equals(a.SourcePort, b.SourcePort, StringComparison.Ordinal)
                || !string.Equals(a.TargetNodeId, b.TargetNodeId, StringComparison.Ordinal)
                || !string.Equals(a.TargetPort, b.TargetPort, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
