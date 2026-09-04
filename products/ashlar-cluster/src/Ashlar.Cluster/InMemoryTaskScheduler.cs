using System.Collections.Concurrent;
using Ashlar.Contracts.Distributed;

namespace Ashlar.Cluster;

/// <summary>
/// Process-local <see cref="ITaskScheduler"/> used by the extractable cluster
/// scaffold. A later increment replaces this with a durable worker queue.
/// </summary>
public sealed class InMemoryTaskScheduler : ITaskScheduler
{
    private readonly ConcurrentDictionary<string, ResultEvidence> _results = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<ScheduledTaskHandle> ScheduleAsync(
        ExecutionEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        var taskId = $"task:{envelope.EnvelopeId}";
        if (_results.ContainsKey(taskId))
        {
            return Task.FromResult(new ScheduledTaskHandle(taskId, envelope.EnvelopeId));
        }

        var evidence = ResultEvidence.Create(
            envelope.EnvelopeId,
            taskId,
            ResultEvidenceStatus.Succeeded,
            envelope.PayloadHash,
            DateTimeOffset.UtcNow);

        _results[taskId] = evidence;
        return Task.FromResult(new ScheduledTaskHandle(taskId, envelope.EnvelopeId));
    }

    /// <inheritdoc />
    public Task<ResultEvidence?> GetResultAsync(
        string taskId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        cancellationToken.ThrowIfCancellationRequested();

        _results.TryGetValue(taskId, out var evidence);
        return Task.FromResult(evidence);
    }
}
