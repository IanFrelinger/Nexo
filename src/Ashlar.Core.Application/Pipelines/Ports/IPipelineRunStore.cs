using Ashlar.Core.Application.Pipelines.Models;

namespace Ashlar.Core.Application.Pipelines.Ports;

/// <summary>
/// Persists pipeline runs for diagnostics and recovery.
/// </summary>
public interface IPipelineRunStore
{
    /// <summary>
    /// Persists or updates a pipeline run aggregate.
    /// </summary>
    /// <param name="run">Run state and stage outcomes to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task SaveAsync(PipelineRun run, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a previously stored run by identifier.
    /// </summary>
    /// <param name="runId">Unique run identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The stored run, or <see langword="null"/> if not found.</returns>
    Task<PipelineRun?> GetAsync(string runId, CancellationToken cancellationToken = default);
}
