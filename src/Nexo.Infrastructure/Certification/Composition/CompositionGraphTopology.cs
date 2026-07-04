using Nexo.Core.Application.Certification.Models;

namespace Nexo.Infrastructure.Certification.Composition;

/// <summary>Computes topological execution order for composition graph nodes.</summary>
internal static class CompositionGraphTopology
{
    /// <summary>Gets execution order.</summary>
    public static IReadOnlyList<string> GetExecutionOrder(CompositionSpec spec)
    {
        var nodeIds = spec.Nodes.Select(n => n.NodeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var incoming = nodeIds.ToDictionary(id => id, _ => 0, StringComparer.OrdinalIgnoreCase);

        foreach (var edge in spec.Edges)
        {
            if (!nodeIds.Contains(edge.TargetNodeId) || !nodeIds.Contains(edge.SourceNodeId))
                continue;

            incoming[edge.TargetNodeId]++;
        }

        var queue = new Queue<string>(incoming.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var order = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            order.Add(current);

            foreach (var edge in spec.Edges.Where(e =>
                         string.Equals(e.SourceNodeId, current, StringComparison.OrdinalIgnoreCase)
                         && nodeIds.Contains(e.TargetNodeId)))
            {
                incoming[edge.TargetNodeId]--;
                if (incoming[edge.TargetNodeId] == 0)
                    queue.Enqueue(edge.TargetNodeId);
            }
        }

        if (order.Count != nodeIds.Count)
            throw new InvalidOperationException("Composition graph contains a cycle or disconnected node");

        return order;
    }
}
