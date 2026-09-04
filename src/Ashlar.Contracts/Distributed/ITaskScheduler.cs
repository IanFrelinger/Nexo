namespace Ashlar.Contracts.Distributed;

/// <summary>
/// Durable task lifecycle port. The framework defines the contract; cluster
/// products implement scheduling, persistence, and worker dispatch.
/// </summary>
public interface ITaskScheduler
{
    /// <summary>
    /// Admits <paramref name="envelope"/> and returns a handle the caller can poll.
    /// </summary>
    /// <param name="envelope">Signed-intent request to schedule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Handle identifying the scheduled task.</returns>
    Task<ScheduledTaskHandle> ScheduleAsync(
        ExecutionEnvelope envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns current evidence for <paramref name="taskId"/>, or <see langword="null"/> if unknown.
    /// </summary>
    /// <param name="taskId">Id returned by <see cref="ScheduleAsync"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ResultEvidence?> GetResultAsync(
        string taskId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Identifier returned by <see cref="ITaskScheduler.ScheduleAsync"/>.
/// </summary>
/// <param name="TaskId">Scheduler-assigned task id.</param>
/// <param name="EnvelopeId">Envelope this task fulfills.</param>
public sealed record ScheduledTaskHandle(string TaskId, string EnvelopeId)
{
    /// <summary>
    /// Validates identifiers on every construction path.
    /// </summary>
    public ScheduledTaskHandle
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(TaskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(EnvelopeId);
        TaskId = TaskId.Trim();
        EnvelopeId = EnvelopeId.Trim();
    }
}
