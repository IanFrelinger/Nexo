using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;
/// <summary>
/// Event source that monitors process start/stop via polling.
/// Emits NormalizedEvent with Category="process-names", SourceId="process-monitor".
/// </summary>
public sealed class ProcessEventSource : IObservableEventSource
{
    private const string SourceIdValue = "process-monitor";
    private const string CategoryValue = "process-names";
    private readonly IReadOnlyList<string> _processFilters;
    private readonly TimeSpan _pollInterval;
    private readonly string? _projectPath;
    private readonly ILogger<ProcessEventSource>? _logger;
    private readonly ConcurrentQueue<NormalizedEvent> _eventQueue = new();
    private readonly SemaphoreSlim _eventAvailable = new(0);
    private readonly ConcurrentDictionary<int, string> _knownProcesses = new();
    /// <summary>
    /// Creates a process event source.
    /// </summary>
    /// <param name = "processFilters">Process names to track (e.g. dotnet, msbuild, nexo). Case-insensitive partial match.</param>
    /// <param name = "projectPath">Optional project path for per-project gating.</param>
    /// <param name = "pollInterval">How often to poll for process changes.</param>
    /// <param name = "logger">Optional logger.</param>
    public ProcessEventSource(IEnumerable<string> processFilters, string? projectPath = null, TimeSpan? pollInterval = null, ILogger<ProcessEventSource>? logger = null)
    {
        _processFilters = processFilters?.Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? throw new ArgumentNullException(nameof(processFilters));
        _projectPath = projectPath;
        _pollInterval = pollInterval ?? TimeSpan.FromSeconds(2);
        _logger = logger;
    }

    /// <inheritdoc/>
    public string SourceId => SourceIdValue;

    /// <inheritdoc/>
    public async IAsyncEnumerable<NormalizedEvent> SubscribeAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_processFilters.Count == 0)
        {
            _logger?.LogWarning("No process filters configured for ProcessEventSource");
            yield break;
        }

        var pollTask = Task.Run(() => PollAsync(cancellationToken), cancellationToken);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_eventQueue.TryDequeue(out var evt))
                {
                    yield return evt;
                }
                else
                {
                    try
                    {
                        await _eventAvailable.WaitAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            await pollTask.ConfigureAwait(false);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var currentIds = new HashSet<int>();
                foreach (var proc in Process.GetProcesses())
                {
                    try
                    {
                        var name = proc.ProcessName;
                        var matches = _processFilters.Any(f => name.Contains(f, StringComparison.OrdinalIgnoreCase));
                        if (matches)
                        {
                            currentIds.Add(proc.Id);
                            if (!_knownProcesses.ContainsKey(proc.Id))
                            {
                                _knownProcesses[proc.Id] = name;
                                Enqueue("started", name, proc.Id);
                            }
                        }
                    }
                    catch
                    {
                        // Swallow per-process exceptions to keep polling
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }

                foreach (var kvp in _knownProcesses.ToList())
                {
                    if (!currentIds.Contains(kvp.Key))
                    {
                        _knownProcesses.TryRemove(kvp.Key, out _);
                        Enqueue("stopped", kvp.Value, kvp.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "ProcessEventSource poll failed");
            }

            await Task.Delay(_pollInterval, ct).ConfigureAwait(false);
        }
    }

    private void Enqueue(string changeType, string processName, int processId)
    {
        var payload = JsonSerializer.SerializeToElement(new { changeType, processName, processId, });
        var evt = new NormalizedEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            SourceId = SourceIdValue,
            Category = CategoryValue,
            ProjectPath = _projectPath,
            Payload = payload,
        };
        _eventQueue.Enqueue(evt);
        _eventAvailable.Release();
    }
}