using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Resolves stages that are ready to execute based on predecessor completion state.
/// </summary>
public sealed class PipelineScheduler : IPipelineScheduler
{
    /// <inheritdoc />
    public IReadOnlyList<string> GetReadyStages(
        PipelineExecutionGraph graph,
        IReadOnlyDictionary<string, PipelineStageRunState> states)
    {
        if (graph == null)
            throw new ArgumentNullException(nameof(graph));
        if (states == null)
            throw new ArgumentNullException(nameof(states));

        var ready = new List<string>();
        var byId = graph.Nodes.ToDictionary(n => n.StageId, StringComparer.Ordinal);

        foreach (var node in graph.Nodes)
        {
            if (!states.TryGetValue(node.StageId, out var current) || current != PipelineStageRunState.Pending)
                continue;

            if (node.Predecessors.Count == 0)
            {
                ready.Add(node.StageId);
                continue;
            }

            var predecessorStates = node.Predecessors
                .Where(p => byId.ContainsKey(p))
                .Select(p => states.TryGetValue(p, out var s) ? s : PipelineStageRunState.Pending)
                .ToList();

            if (node.StageType == PipelineStageType.FanIn)
            {
                if (IsFanInReady(node, predecessorStates))
                    ready.Add(node.StageId);
                continue;
            }

            if (predecessorStates.All(s => s == PipelineStageRunState.Completed))
                ready.Add(node.StageId);
        }

        return ready;
    }

    private static bool IsFanInReady(PipelineExecutionNode node, IReadOnlyList<PipelineStageRunState> predecessorStates)
    {
        var completed = predecessorStates.Count(s => s == PipelineStageRunState.Completed);

        if (node.JoinStrategy == PipelineJoinStrategy.FirstSuccess)
            return completed >= 1;

        if (node.JoinStrategy == PipelineJoinStrategy.Quorum)
        {
            var quorum = node.QuorumCount.GetValueOrDefault(0);
            return quorum > 0 && completed >= quorum;
        }

        return predecessorStates.All(s => s == PipelineStageRunState.Completed);
    }
}
