using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Core.Application.Pipelines.Ports;

/// <summary>
/// Produces scaling decisions for deterministic and agentic worker pools.
/// </summary>
public interface IPipelineScalingPolicy
{
    /// <summary>
    /// Evaluates queue depth and error rates to recommend worker pool sizes.
    /// </summary>
    /// <param name="snapshot">Current queue depths, error rates, and worker counts.</param>
    /// <returns>Target worker counts and optional parallelism limits.</returns>
    PipelineScalingDecision Evaluate(PipelineScalingSnapshot snapshot);
}
