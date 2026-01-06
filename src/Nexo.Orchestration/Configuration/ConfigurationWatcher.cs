using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Nexo.Orchestration.Configuration;

/// <summary>
/// Watches configuration files for changes and triggers reload.
/// 
/// Responsibilities:
/// - Monitors configuration files for changes using FileSystemWatcher
/// - Automatically reloads configuration when files change
/// - Raises ConfigurationChanged events
/// - Supports common configuration file names (appsettings.json, nexo.json, etc.)
/// 
/// Implements IDisposable for proper cleanup of file watchers.
/// </summary>
public sealed class ConfigurationWatcher : IDisposable
{
    private readonly IConfigurationRoot _configuration;
    private readonly ILogger<ConfigurationWatcher>? _logger;
    private readonly FileSystemWatcher? _watcher;
    private readonly string? _configPath;
    private bool _disposed;

    public ConfigurationWatcher(
        IConfigurationRoot configuration,
        ILogger<ConfigurationWatcher>? logger = null)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;

        // Try to find configuration file path
        _configPath = FindConfigurationPath();
        
        if (_configPath != null)
        {
            var directory = Path.GetDirectoryName(_configPath);
            var fileName = Path.GetFileName(_configPath);

            if (directory != null)
            {
                _watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnConfigurationChanged;
                _logger?.LogInformation("Watching configuration file: {Path}", _configPath);
            }
        }
    }

    /// <summary>
    /// Event raised when configuration changes.
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    private void OnConfigurationChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Small delay to ensure file is fully written
            System.Threading.Thread.Sleep(100);

            _configuration.Reload();
            
            _logger?.LogInformation("Configuration file changed: {Path}. Reloaded configuration.", e.FullPath);

            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                FilePath = e.FullPath,
                ChangeType = e.ChangeType.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to reload configuration from {Path}", e.FullPath);
        }
    }

    private string? FindConfigurationPath()
    {
        // Common configuration file names
        var configFiles = new[]
        {
            "appsettings.json",
            "appsettings.Development.json",
            "nexo.json",
            "nexo.config.json"
        };

        var currentDirectory = Directory.GetCurrentDirectory();
        
        foreach (var configFile in configFiles)
        {
            var path = Path.Combine(currentDirectory, configFile);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _watcher?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Event arguments for configuration change events.
/// 
/// Contains:
/// - File path that changed
/// - Change type (Created, Changed, Deleted, etc.)
/// 
/// Used by ConfigurationWatcher.ConfigurationChanged event.
/// </summary>
public sealed class ConfigurationChangedEventArgs : EventArgs
{
    public required string FilePath { get; init; }
    public required string ChangeType { get; init; }
}

