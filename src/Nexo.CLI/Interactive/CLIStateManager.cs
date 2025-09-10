using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Manages persistent CLI state including user preferences, command history, and context
    /// </summary>
    public class CLIStateManager : ICLIStateManager
    {
        private readonly string _stateFilePath;
        private readonly ILogger<CLIStateManager> _logger;
        private CLIState _currentState;
        
        public CLIStateManager(IConfiguration configuration, ILogger<CLIStateManager> logger)
        {
            _logger = logger;
            _stateFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".nexo",
                "cli-state.json"
            );
            
            _currentState = LoadState();
        }
        
        public Task<CLIContext> GetCurrentContextAsync()
    {
        return Task.FromResult(new CLIContext
            {
                CurrentProject = _currentState.CurrentProject,
                CurrentPlatform = _currentState.CurrentPlatform,
                RecentCommands = _currentState.CommandHistory.TakeLast(10).Select(h => h.Command).ToList(),
                UserPreferences = _currentState.UserPreferences,
                HasActiveMonitoring = await CheckActiveMonitoring(),
                HasPendingAdaptations = await CheckPendingAdaptations(),
                HasPerformanceIssues = await CheckPerformanceIssues(),
                WorkingDirectory = Directory.GetCurrentDirectory(),
                LastActivity = _currentState.LastUsed
            });
    }
        
        public async Task SetCurrentProjectAsync(ProjectInfo project)
        {
            _currentState.CurrentProject = project;
            await SaveStateAsync();
            _logger.LogInformation("Set current project to: {ProjectName}", project.Name);
        }
        
        public async Task SetCurrentPlatformAsync(string platform)
        {
            _currentState.CurrentPlatform = platform;
            await SaveStateAsync();
            _logger.LogInformation("Set current platform to: {Platform}", platform);
        }
        
        public async Task AddToHistoryAsync(string command)
        {
            var historyEntry = new CommandHistoryEntry
            {
                Command = command,
                Timestamp = DateTime.UtcNow,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                Successful = true, // This would be determined by command execution result
                ExecutionTime = TimeSpan.Zero // This would be measured during execution
            };
            
            _currentState.CommandHistory.Add(historyEntry);
            
            // Keep only last 1000 commands
            if (_currentState.CommandHistory.Count > 1000)
            {
                _currentState.CommandHistory.RemoveAt(0);
            }
            
            _currentState.LastUsed = DateTime.UtcNow;
            await SaveStateAsync();
        }
        
        public async Task<List<string>> GetCommandHistoryAsync()
        {
            return _currentState.CommandHistory
                .OrderByDescending(h => h.Timestamp)
                .Select(h => h.Command)
                .ToList();
        }
        
        public async Task UpdateUserPreferenceAsync(string key, object value)
        {
            _currentState.UserPreferences[key] = value;
            await SaveStateAsync();
            _logger.LogInformation("Updated user preference: {Key} = {Value}", key, value);
        }
        
        public async Task<T?> GetUserPreferenceAsync<T>(string key)
        {
            if (_currentState.UserPreferences.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T directValue)
                    {
                        return directValue;
                    }
                    
                    // Try to convert using JSON serialization
                    var json = JsonSerializer.Serialize(value);
                    return JsonSerializer.Deserialize<T>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert preference {Key} to type {Type}", key, typeof(T).Name);
                }
            }
            
            return default(T);
        }
        
        public async Task ClearHistoryAsync()
        {
            _currentState.CommandHistory.Clear();
            await SaveStateAsync();
            _logger.LogInformation("Cleared command history");
        }
        
        public async Task SaveStateAsync()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
                
                var json = JsonSerializer.Serialize(_currentState, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(_stateFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save CLI state");
            }
        }
        
        private CLIState LoadState()
        {
            try
            {
                if (File.Exists(_stateFilePath))
                {
                    var json = File.ReadAllText(_stateFilePath);
                    var state = JsonSerializer.Deserialize<CLIState>(json);
                    if (state != null)
                    {
                        _logger.LogInformation("Loaded CLI state from: {FilePath}", _stateFilePath);
                        return state;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load CLI state, using default");
            }
            
            return new CLIState();
        }
        
        private async Task<bool> CheckActiveMonitoring()
        {
            // This would check if any monitoring services are active
            // For now, return false
            await Task.CompletedTask;
            return false;
        }
        
        private async Task<bool> CheckPendingAdaptations()
        {
            // This would check if there are pending adaptations
            // For now, return false
            await Task.CompletedTask;
            return false;
        }
        
        private async Task<bool> CheckPerformanceIssues()
        {
            // This would check for performance issues
            // For now, return false
            await Task.CompletedTask;
            return false;
        }
    }
    
    /// <summary>
    /// Represents the persistent CLI state
    /// </summary>
    public class CLIState
    {
        public ProjectInfo? CurrentProject { get; set; }
        public string? CurrentPlatform { get; set; }
        public List<CommandHistoryEntry> CommandHistory { get; set; } = new();
        public Dictionary<string, object> UserPreferences { get; set; } = new();
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0.0";
    }
}
