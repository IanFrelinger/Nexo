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
        ArgumentException.ThrowIfNullOrWhiteSpace(envelope.EnvelopeId);

        var taskId = $"task:{envelope.EnvelopeId}";
        var handle = ScheduledTaskHandle.Create(taskId, envelope.EnvelopeId);
        var evidence = ResultEvidence.Create(
            envelope.EnvelopeId,
            taskId,
            ResultEvidenceStatus.Succeeded,
            envelope.PayloadHash,
            DateTimeOffset.UtcNow);

        if (_results.TryAdd(taskId, evidence))
        {
            return Task.FromResult(handle);
        }

        if (!_results.TryGetValue(taskId, out var existing))
        {
            throw new InvalidOperationException(
                $"Envelope '{envelope.EnvelopeId}' was dropped during schedule.");
        }

        if (!string.Equals(existing.OutputHash, envelope.PayloadHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Envelope '{envelope.EnvelopeId}' is already scheduled with a different payload hash.");
        }

        return Task.FromResult(handle);
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
