using Ashlar.Core.Domain;
using Ashlar.Core.Domain.Execution;

namespace Ashlar.Core.Application.Execution.Routing;

/// <summary>
/// Discriminated union representing where a job will actually run.
/// Produced by <see cref="ICapabilityRouter.ResolveExecutionTarget"/>
/// and consumed by the pipeline stage that dispatches the job.
/// <para>
/// <c>Reason</c> is a human-readable explanation of why this target was
/// chosen (e.g. "local GPU meets VRAM requirement", "peer abc123 trusted,
/// lower latency than cloud") — useful for debugging routing decisions.
/// </para>
/// </summary>
public abstract record ExecutionTarget
{
    private ExecutionTarget()
    {
    }

    /// <summary>Job will execute on the local node.</summary>
    /// <param name="Executor">Local executor that runs the job.</param>
    /// <param name="Reason">Human-readable explanation of the routing decision.</param>
    public sealed record Local(ILocalExecutor Executor, string Reason) : ExecutionTarget;

    /// <summary>Job will execute on a remote target (peer or RunPod cloud).</summary>
    /// <param name="Executor">Remote executor that runs the job.</param>
    /// <param name="Reason">Human-readable explanation of the routing decision.</param>
    public sealed record Remote(IBrickExecutor Executor, string Reason) : ExecutionTarget;
}
