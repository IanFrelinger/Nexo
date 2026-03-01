using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;
/// <summary>
/// Detects patterns in the normalized event stream using sliding-window heuristics.
/// Emits ObservedPattern when thresholds are met (e.g. repeated-edits, edit-then-build).
/// </summary>
public sealed class PatternDetector
{
    private readonly TimeSpan _windowSize;
    private readonly int _repeatedEditThreshold;
    private readonly IPatternStore _store;
    private readonly ILogger<PatternDetector>? _logger;
    private readonly ConcurrentQueue<NormalizedEvent> _window = new();
    private DateTimeOffset _windowStart = DateTimeOffset.UtcNow;
    /// <summary>
    /// Creates a pattern detector.
    /// </summary>
    /// <param name = "windowSize">Sliding window duration (e.g. 5 min).</param>
    /// <param name = "repeatedEditThreshold">Minimum file changes to same path to emit repeated-edits.</param>
    /// <param name = "store">Pattern store to write detected patterns to.</param>
    /// <param name = "logger">Optional logger.</param>
    public PatternDetector(TimeSpan windowSize, int repeatedEditThreshold, IPatternStore store, ILogger<PatternDetector>? logger = null)
    {
        _windowSize = windowSize;
        _repeatedEditThreshold = repeatedEditThreshold;
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger;
    }

    /// <summary>
    /// Process an event and potentially emit a pattern.
    /// </summary>
    public async Task ProcessAsync(NormalizedEvent evt, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PruneWindow();
        _window.Enqueue(evt);
        if (evt.SourceId == "file-system" && evt.Category == "file-paths")
        {
            await DetectRepeatedEditsAsync(cancellationToken).ConfigureAwait(false);
        }

        if (evt.SourceId == "process-monitor" && evt.Category == "process-names")
        {
            await DetectEditThenBuildAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void PruneWindow()
    {
        var cutoff = DateTimeOffset.UtcNow - _windowSize;
        while (_window.Count > 0 && _window.TryPeek(out var first) && first.Timestamp < cutoff)
        {
            _window.TryDequeue(out _);
        }
    }

    private async Task DetectRepeatedEditsAsync(CancellationToken ct)
    {
        var pathCounts = new Dictionary<string, List<NormalizedEvent>>(StringComparer.OrdinalIgnoreCase);
        foreach (var evt in _window)
        {
            if (evt.SourceId != "file-system" || evt.Category != "file-paths")
                continue;
            var path = GetPathFromPayload(evt);
            if (string.IsNullOrEmpty(path))
                continue;
            if (!pathCounts.TryGetValue(path, out var list))
            {
                list = new List<NormalizedEvent>();
                pathCounts[path] = list;
            }

            list.Add(evt);
        }

        foreach (var(path, events)in pathCounts)
        {
            if (events.Count >= _repeatedEditThreshold)
            {
                var first = events[0];
                var last = events[^1];
                var pattern = new ObservedPattern
                {
                    PatternId = Guid.NewGuid().ToString("N"),
                    EventType = "repeated-edits",
                    Frequency = events.Count,
                    FirstSeen = first.Timestamp,
                    LastSeen = last.Timestamp,
                    ProjectPath = first.ProjectPath,
                    RelatedEventIds = events.Select(e => e.EventId).ToList(),
                    Metadata = JsonSerializer.SerializeToElement(new { path, intervalSeconds = (last.Timestamp - first.Timestamp).TotalSeconds }),
                };
                await _store.AddAsync(pattern, ct).ConfigureAwait(false);
                _logger?.LogInformation("Detected pattern: {Type} for {Path} ({Count} edits)", pattern.EventType, path, events.Count);
            }
        }
    }

    private async Task DetectEditThenBuildAsync(CancellationToken ct)
    {
        var events = _window.ToArray();
        var fileEvents = events.Where(e => e.SourceId == "file-system").OrderBy(e => e.Timestamp).ToList();
        var processEvents = events.Where(e => e.SourceId == "process-monitor").OrderBy(e => e.Timestamp).ToList();
        foreach (var proc in processEvents)
        {
            var procName = GetProcessNameFromPayload(proc);
            if (string.IsNullOrEmpty(procName) || (procName != "dotnet" && procName != "msbuild" && !procName.Contains("nexo")))
                continue;
            var recentFileEvents = fileEvents.Where(f => f.Timestamp < proc.Timestamp && (proc.Timestamp - f.Timestamp).TotalSeconds < 120).ToList();
            if (recentFileEvents.Count > 0)
            {
                var first = recentFileEvents[0];
                var pattern = new ObservedPattern
                {
                    PatternId = Guid.NewGuid().ToString("N"),
                    EventType = "edit-then-build",
                    Frequency = 1,
                    FirstSeen = first.Timestamp,
                    LastSeen = proc.Timestamp,
                    ProjectPath = first.ProjectPath ?? proc.ProjectPath,
                    RelatedEventIds = recentFileEvents.Take(5).Select(e => e.EventId).Concat(new[] { proc.EventId }).ToList(),
                    Metadata = JsonSerializer.SerializeToElement(new { processName = procName, fileCount = recentFileEvents.Count }),
                };
                await _store.AddAsync(pattern, ct).ConfigureAwait(false);
                _logger?.LogInformation("Detected pattern: {Type} ({Process})", pattern.EventType, procName);
            }
        }
    }

    private static string? GetPathFromPayload(NormalizedEvent evt)
    {
        if (!evt.Payload.HasValue)
            return null;
        try
        {
            var doc = evt.Payload.Value;
            if (doc.TryGetProperty("fullPath", out var pathProp))
                return pathProp.GetString();
        }
        catch
        {
            System.Diagnostics.Trace.WriteLine("Caught");
        }

        return null;
    }

    private static string? GetProcessNameFromPayload(NormalizedEvent evt)
    {
        if (!evt.Payload.HasValue)
            return null;
        try
        {
            var doc = evt.Payload.Value;
            if (doc.TryGetProperty("processName", out var prop))
                return prop.GetString();
        }
        catch
        {
            System.Diagnostics.Trace.WriteLine("Caught");
        }

        return null;
    }
}