using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Director.Core.Protocol;

namespace Director.Avalonia.Services;

/// <summary>
/// Reads events from files instead of network sockets for sandboxed local testing
/// </summary>
public sealed class FileBasedEventReader : IDisposable
{
    private readonly ILogger<FileBasedEventReader> _logger;
    private readonly string _communicationDirectory;
    private readonly FileSystemWatcher _fileWatcher;
    private readonly Timer _pollTimer;
    private bool _disposed = false;
    private readonly HashSet<string> _processedFiles = new();

    public event EventHandler<DirectorEvent>? EventReceived;

    public FileBasedEventReader(ILogger<FileBasedEventReader> logger, string communicationDirectory)
    {
        _logger = logger;
        _communicationDirectory = communicationDirectory;
        
        // Create directory if it doesn't exist
        Directory.CreateDirectory(_communicationDirectory);
        
        // Set up file system watcher
        _fileWatcher = new FileSystemWatcher(_communicationDirectory, "event_*.json")
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite
        };
        
        _fileWatcher.Created += OnFileCreated;
        _fileWatcher.Changed += OnFileChanged;
        
        // Set up polling timer as backup
        _pollTimer = new Timer(PollForNewFiles, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        
        _logger.LogInformation($"File-based event reader initialized for directory: {_communicationDirectory}");
    }

    /// <summary>
    /// Starts reading events from the communication directory
    /// </summary>
    public async Task StartReadingAsync()
    {
        _logger.LogInformation("Starting file-based event reading...");
        
        // Process any existing files
        await ProcessExistingFilesAsync();
        
        _logger.LogInformation("File-based event reading started");
    }

    /// <summary>
    /// Processes any existing event files in the directory
    /// </summary>
    private async Task ProcessExistingFilesAsync()
    {
        try
        {
            var files = Directory.GetFiles(_communicationDirectory, "event_*.json")
                .OrderBy(f => File.GetCreationTime(f))
                .ToArray();

            foreach (var file in files)
            {
                await ProcessEventFileAsync(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing existing files");
        }
    }

    /// <summary>
    /// Polls for new files as a backup mechanism
    /// </summary>
    private async void PollForNewFiles(object? state)
    {
        if (_disposed) return;

        try
        {
            var files = Directory.GetFiles(_communicationDirectory, "event_*.json")
                .Where(f => !_processedFiles.Contains(f))
                .OrderBy(f => File.GetCreationTime(f))
                .ToArray();

            foreach (var file in files)
            {
                await ProcessEventFileAsync(file);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling for new files");
        }
    }

    /// <summary>
    /// Handles file creation events
    /// </summary>
    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath != null)
        {
            await ProcessEventFileAsync(e.FullPath);
        }
    }

    /// <summary>
    /// Handles file change events
    /// </summary>
    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath != null)
        {
            await ProcessEventFileAsync(e.FullPath);
        }
    }

    /// <summary>
    /// Processes a single event file
    /// </summary>
    private async Task ProcessEventFileAsync(string filePath)
    {
        if (_processedFiles.Contains(filePath))
            return;

        try
        {
            // Wait a bit to ensure file is fully written
            await Task.Delay(100);
            
            if (!File.Exists(filePath))
                return;

            var json = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var evt = JsonSerializer.Deserialize<DirectorEvent>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (evt != null)
            {
                _processedFiles.Add(filePath);
                EventReceived?.Invoke(this, evt);
                _logger.LogDebug($"Processed event from file: {Path.GetFileName(filePath)}");
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, $"Failed to parse JSON from file: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing event file: {filePath}");
        }
    }

    /// <summary>
    /// Gets the communication directory path
    /// </summary>
    public string CommunicationDirectory => _communicationDirectory;

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _fileWatcher?.Dispose();
            _pollTimer?.Dispose();
            _logger.LogInformation("File-based event reader disposed");
        }
    }
}
