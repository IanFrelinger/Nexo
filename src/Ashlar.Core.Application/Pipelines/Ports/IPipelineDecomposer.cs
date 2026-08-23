using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Core.Application.Pipelines.Ports;

/// <summary>
/// Decomposes a validated template into an execution graph.
/// </summary>
public interface IPipelineDecomposer
{
    /// <summary>
    /// Materializes an execution graph from a validated template.
    /// </summary>
    /// <param name="template">Pipeline template with stages and edges.</param>
    /// <returns>Graph of execution nodes ready for scheduling.</returns>
    PipelineExecutionGraph Decompose(PipelineTemplate template);
}
