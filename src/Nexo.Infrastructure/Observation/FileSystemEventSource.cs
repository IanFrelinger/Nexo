using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Observation.Models;
using Nexo.Core.Application.Observation.Ports;

namespace Nexo.Infrastructure.Observation;

/// <summary>
/// Event source that watches the file system using FileSystemWatcher.
/// Emits NormalizedEvent with Category="file-paths", SourceId="file-system".
/// </summary>
public sealed class FileSystemEventSource : IObservableEventSource, IDisposable
{
    private const string SourceIdValue = "file-system";
    private const string CategoryValue = "file-paths";

    private readonly IReadOnlyList<string> _watchPaths;
    private readonly IReadOnlyList<string> _filters;
    private readonly string? _projectPath;
    private readonly ILogger<FileSystemEventSource>? _logger;
    private readonly ConcurrentQueue<NormalizedEvent> _eventQueue = new();
    private readonly SemaphoreSlim _eventAvailable = new(0);
    private readonly List<FileSystemWatcher> _watchers = new();
    private bool _disposed;

    /// <summary>
    /// Creates a file system event source.
    /// </summary>
    /// <param name="watchPaths">Directories to watch (e.g. src/, .github/).</param>
    /// <param name="projectPath">Optional project path for per-project gating.</param>
    /// <param name="filters">Optional file filters (e.g. *.cs, *.csproj). Default: *.</param>
    /// <param name="logger">Optional logger.</param>
    public FileSystemEventSource(
        IEnumerable<string> watchPaths,
        string? projectPath = null,
        IEnumerable<string>? filters = null,
        ILogger<FileSystemEventSource>? logger = null)
    {
        _watchPaths = watchPaths?.Where(p => !string.IsNullOrWhiteSpace(p)).Select(Path.GetFullPath).Distinct().ToList()
            ?? throw new ArgumentNullException(nameof(watchPaths));
        _projectPath = projectPath;
        _filters = filters?.ToList() ?? new List<string> { "*" };
        _logger = logger;
    }

    /// <inheritdoc />
    public string SourceId => SourceIdValue;

    /// <inheritdoc />
    public async IAsyncEnumerable<NormalizedEvent> SubscribeAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_watchPaths.Count == 0)
        {
            _logger?.LogWarning("No watch paths configured for FileSystemEventSource");
            yield break;
        }

        foreach (var watchPath in _watchPaths)
        {
            if (!Directory.Exists(watchPath))
            {
                _logger?.LogWarning("Watch path does not exist: {Path}", watchPath);
                continue;
            }

            foreach (var filter in _filters)
            {
                var watcher = new FileSystemWatcher(watchPath)
                {
                    Filter = filter,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                };
                watcher.Created += OnFileSystemEvent;
                watcher.Changed += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Renamed += OnRenamed;
                watcher.EnableRaisingEvents = true;
                _watchers.Add(watcher);
            }
        }

        _logger?.LogInformation("FileSystemEventSource watching {Count} path(s)", _watchPaths.Count);

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
                        await _eventAvailable.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
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
            foreach (var w in _watchers)
            {
                w.EnableRaisingEvents = false;
                w.Created -= OnFileSystemEvent;
                w.Changed -= OnFileSystemEvent;
                w.Deleted -= OnFileSystemEvent;
                w.Renamed -= OnRenamed;
                w.Dispose();
            }
            _watchers.Clear();
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        var changeType = e.ChangeType.ToString().ToLowerInvariant();
        var payload = JsonSerializer.SerializeToElement(new
        {
            changeType,
            fullPath = e.FullPath,
            name = e.Name,
        });
        Enqueue(CategoryValue, changeType, e.FullPath, payload);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        var payload = JsonSerializer.SerializeToElement(new
        {
            changeType = "renamed",
            fullPath = e.FullPath,
            name = e.Name,
            oldFullPath = e.OldFullPath,
            oldName = e.OldName,
        });
        Enqueue(CategoryValue, "renamed", e.FullPath, payload);
    }

    private void Enqueue(string category, string changeType, string fullPath, JsonElement payload)
    {
        var evt = new NormalizedEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            SourceId = SourceIdValue,
            Category = category,
            ProjectPath = _projectPath,
            Payload = payload,
        };
        _eventQueue.Enqueue(evt);
        _eventAvailable.Release();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        foreach (var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        _watchers.Clear();
        _eventAvailable.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
