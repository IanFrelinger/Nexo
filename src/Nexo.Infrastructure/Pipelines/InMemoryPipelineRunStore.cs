using System.Collections.Concurrent;
using Nexo.Core.Application.Pipelines.Models;
using Nexo.Core.Application.Pipelines.Ports;

namespace Nexo.Infrastructure.Pipelines;

/// <summary>
/// Simple in-memory run store for pipeline execution tests and local runtime.
/// </summary>
public sealed class InMemoryPipelineRunStore : IPipelineRunStore
{
    private readonly ConcurrentDictionary<string, PipelineRun> _runs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Save asynchronously.</summary>
    public Task SaveAsync(PipelineRun run, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _runs[run.RunId] = run;
        return Task.CompletedTask;
    }

    /// <summary>Get asynchronously.</summary>
    public Task<PipelineRun?> GetAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _runs.TryGetValue(runId, out var run);
        return Task.FromResult(run);
    }
}
