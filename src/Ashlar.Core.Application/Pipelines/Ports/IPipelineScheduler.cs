using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Core.Application.Pipelines.Ports;

/// <summary>
/// Evaluates whether a stage can be scheduled based on predecessor state.
/// </summary>
public interface IPipelineScheduler
{
    /// <summary>
    /// Returns stage identifiers whose predecessors have completed and are ready to run.
    /// </summary>
    /// <param name="graph">Executable graph with predecessor and successor edges.</param>
    /// <param name="states">Current runtime state per stage identifier.</param>
    /// <returns>Stage identifiers eligible for dispatch in this scheduling pass.</returns>
    IReadOnlyList<string> GetReadyStages(
        PipelineExecutionGraph graph,
        IReadOnlyDictionary<string, PipelineStageRunState> states);
}
