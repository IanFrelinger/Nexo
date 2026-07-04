using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>
/// Derives structural graph mutants (reorder/drop/redirect/swap). Does not mutate brick source.
/// </summary>
internal static class CompositionGraphMutationCatalog
{
    private const int MaxPerKind = 4;

    /// <summary>Collect mutations.</summary>
    public static IReadOnlyList<StructuralMutation> CollectMutations(
        CompositionSpec spec,
        IBrickRegistry brickRegistry)
    {
        var mutations = new List<StructuralMutation>();
        CollectReorderAdjacent(spec, mutations);
        CollectDropNode(spec, mutations);
        CollectRedirectEdge(spec, mutations);
        CollectSwapBrick(spec, brickRegistry, mutations);
        return mutations;
    }

    private static void CollectReorderAdjacent(CompositionSpec spec, List<StructuralMutation> mutations)
    {
        if (spec.Nodes.Count < 2)
            return;

        var order = CompositionGraphTopology.GetExecutionOrder(spec);
        var count = 0;
        for (var i = 0; i < order.Count - 1 && count < MaxPerKind; i++)
        {
            var leftId = order[i];
            var rightId = order[i + 1];
            var left = spec.Nodes.First(n => string.Equals(n.NodeId, leftId, StringComparison.Ordinal));
            var right = spec.Nodes.First(n => string.Equals(n.NodeId, rightId, StringComparison.Ordinal));

            var nodes = spec.Nodes
                .Select(n =>
                {
                    if (string.Equals(n.NodeId, leftId, StringComparison.Ordinal))
                        return n with { BrickId = right.BrickId };
                    if (string.Equals(n.NodeId, rightId, StringComparison.Ordinal))
                        return n with { BrickId = left.BrickId };
                    return n;
                })
                .ToList();

            mutations.Add(new StructuralMutation($"reorder-adjacent-{i}", spec with { Nodes = nodes }));
            count++;
        }
    }

    private static void CollectDropNode(CompositionSpec spec, List<StructuralMutation> mutations)
    {
        if (spec.Nodes.Count < 2)
            return;

        var count = 0;
        foreach (var node in spec.Nodes)
        {
            if (count >= MaxPerKind)
                break;

            var remainingNodes = spec.Nodes.Where(n => !string.Equals(n.NodeId, node.NodeId, StringComparison.Ordinal)).ToList();
            var remainingEdges = spec.Edges
                .Where(e => !string.Equals(e.SourceNodeId, node.NodeId, StringComparison.Ordinal)
                            && !string.Equals(e.TargetNodeId, node.NodeId, StringComparison.Ordinal))
                .ToList();

            var mutant = spec with { Nodes = remainingNodes, Edges = remainingEdges };
            mutations.Add(new StructuralMutation($"drop-node-{node.NodeId}", mutant));
            count++;
        }
    }

    private static void CollectRedirectEdge(CompositionSpec spec, List<StructuralMutation> mutations)
    {
        var count = 0;
        foreach (var edge in spec.Edges)
        {
            if (count >= MaxPerKind)
                break;

            if (string.Equals(edge.TargetNodeId, CompositionGraphConstants.OutputNodeId, StringComparison.Ordinal))
                continue;

            var alternateTargets = spec.Nodes
                .Where(n => !string.Equals(n.NodeId, edge.TargetNodeId, StringComparison.Ordinal))
                .Select(n => n.NodeId)
                .ToList();

            if (alternateTargets.Count == 0)
                continue;

            var redirected = edge with { TargetNodeId = alternateTargets[0] };
            var edges = spec.Edges
                .Select(e => string.Equals(e.SourceNodeId, edge.SourceNodeId, StringComparison.Ordinal)
                             && string.Equals(e.SourcePort, edge.SourcePort, StringComparison.Ordinal)
                             && string.Equals(e.TargetNodeId, edge.TargetNodeId, StringComparison.Ordinal)
                             && string.Equals(e.TargetPort, edge.TargetPort, StringComparison.Ordinal)
                    ? redirected
                    : e)
                .ToList();

            mutations.Add(new StructuralMutation(
                $"redirect-edge-{edge.SourceNodeId}-{edge.SourcePort}-to-{alternateTargets[0]}",
                spec with { Edges = edges }));
            count++;
        }
    }

    private static void CollectSwapBrick(
        CompositionSpec spec,
        IBrickRegistry brickRegistry,
        List<StructuralMutation> mutations)
    {
        var count = 0;
        foreach (var node in spec.Nodes)
        {
            if (count >= MaxPerKind)
                break;

            var current = brickRegistry.GetBrick(node.BrickId);
            if (current is null)
                continue;

            foreach (var candidate in brickRegistry.GetAllBricks())
            {
                if (string.Equals(candidate.Id, node.BrickId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!HasCompatibleInterface(current, candidate))
                    continue;

                var nodes = spec.Nodes
                    .Select(n => string.Equals(n.NodeId, node.NodeId, StringComparison.Ordinal)
                        ? n with { BrickId = candidate.Id }
                        : n)
                    .ToList();

                mutations.Add(new StructuralMutation(
                    $"swap-brick-{node.NodeId}-to-{candidate.Id}",
                    spec with { Nodes = nodes }));
                count++;
                break;
            }
        }
    }

    private static bool HasCompatibleInterface(DomainBrick current, DomainBrick candidate)
    {
        if (current.Interface.Inputs.Count != candidate.Interface.Inputs.Count
            || current.Interface.Outputs.Count != candidate.Interface.Outputs.Count)
            return false;

        foreach (var input in current.Interface.Inputs)
        {
            var match = candidate.Interface.Inputs.FirstOrDefault(i =>
                string.Equals(i.Name, input.Name, StringComparison.OrdinalIgnoreCase));
            if (match is null || !string.Equals(match.Type, input.Type, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        foreach (var output in current.Interface.Outputs)
        {
            var match = candidate.Interface.Outputs.FirstOrDefault(o =>
                string.Equals(o.Name, output.Name, StringComparison.OrdinalIgnoreCase));
            if (match is null || !string.Equals(match.Type, output.Type, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
