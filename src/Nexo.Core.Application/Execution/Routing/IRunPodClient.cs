using Nexo.Core.Domain;
using Nexo.Core.Domain.Execution;

namespace Nexo.Core.Application.Execution.Routing;

/// <summary>
/// Abstraction over the RunPod serverless API.
/// <para>
/// Lifecycle: <see cref="SpinUpInstance"/> → <see cref="DispatchJob"/> →
/// poll <see cref="PollJobStatus"/> until terminal → <see cref="PullResults"/>
/// → <see cref="TerminateInstance"/>.
/// </para>
/// <para>
/// Implementations may pool instances across callers; instance IDs are opaque
/// and should not be parsed.
/// </para>
/// </summary>
public interface IRunPodClient
{
    /// <summary>Provisions a GPU instance with the requested model loaded.</summary>
    /// <param name="modelId">Model to load on the instance.</param>
    /// <param name="gpuType">RunPod GPU tier identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Provisioned instance details or an error.</returns>
    Task<Result<RunPodInstance>> SpinUpInstance(
        string modelId,
        string gpuType,
        CancellationToken cancellationToken = default);

    /// <summary>Dispatches a generation job to a running instance.</summary>
    /// <param name="instanceId">Target instance identifier.</param>
    /// <param name="payload">Generation prompt and parameters.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Opaque job handle for polling and result retrieval.</returns>
    Task<Result<JobHandle>> DispatchJob(
        string instanceId,
        RunPodJobPayload payload,
        CancellationToken cancellationToken = default);

    /// <summary>Polls the current status of a dispatched job.</summary>
    /// <param name="jobHandle">Handle returned by <see cref="DispatchJob"/>.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Current job state; stop polling when <see cref="JobStatus.IsTerminal"/> is true.</returns>
    Task<Result<JobStatus>> PollJobStatus(
        JobHandle jobHandle,
        CancellationToken cancellationToken = default);

    /// <summary>Downloads the output bytes for a completed job.</summary>
    /// <param name="jobHandle">Handle for the completed job.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Raw output payload bytes.</returns>
    Task<Result<byte[]>> PullResults(
        JobHandle jobHandle,
        CancellationToken cancellationToken = default);

    /// <summary>Tears down a provisioned GPU instance.</summary>
    /// <param name="instanceId">Instance to terminate.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<Result<Unit>> TerminateInstance(
        string instanceId,
        CancellationToken cancellationToken = default);
}
