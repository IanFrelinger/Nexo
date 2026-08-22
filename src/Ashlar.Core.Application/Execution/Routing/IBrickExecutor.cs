using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Common interface for any component that can execute a generation job,
/// whether locally, on a peer, or in the cloud.  <see cref="ILocalExecutor"/>
/// and <see cref="IPeerExecutor"/> are marker sub-interfaces that let DI
/// and the router distinguish execution venue without runtime type-checks.
/// </summary>
public interface IBrickExecutor
{
    /// <summary>
    /// Executes a generation job and returns a unified result regardless of venue.
    /// </summary>
    /// <param name="payload">Model, prompt, and generation parameters.</param>
    /// <param name="requirements">Resource and routing requirements for the job.</param>
    /// <param name="context">Execution context for tracing and policy.</param>
    /// <param name="cancellationToken">Token used to cancel execution.</param>
    /// <returns>Success with <see cref="GenerationExecutionResult"/> or failure with <see cref="Error"/>.</returns>
    Task<Result<GenerationExecutionResult>> ExecuteAsync(
        RunPodJobPayload payload,
        JobRequirements requirements,
        IExecutionContext context,
        CancellationToken cancellationToken = default);
}
