using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Execution.Models;
using Nexo.Core.Application.Execution.Ports;
using Nexo.Core.Application.Paths;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Persists step execution mode to JSON file. Swap takes effect on next execution.
/// </summary>
public sealed class StepExecutionModeStore : IStepExecutionMode
{
    private readonly string _configPath;
    private readonly ILogger<StepExecutionModeStore>? _logger;
    private readonly Dictionary<string, ExecutionMode> _modes = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>Initializes a new step execution mode store.</summary>
    public StepExecutionModeStore(string? configPath = null, ILogger<StepExecutionModeStore>? logger = null)
    {
        _configPath = configPath ?? Path.Combine(RepoPathResolver.FindRepoRoot(), "nexo-step-modes.json");
        _logger = logger;
        Load();
    }

    /// <summary>Gets mode.</summary>
    public ExecutionMode GetMode(string stepId)
    {
        lock (_lock)
        {
            return _modes.TryGetValue(stepId, out var mode) ? mode : ExecutionMode.Deterministic;
        }
    }

    /// <summary>Set mode asynchronously.</summary>
    public Task SetModeAsync(string stepId, ExecutionMode mode, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _modes[stepId] = mode;
            Save();
        }
        return Task.CompletedTask;
    }

    /// <summary>Swap asynchronously.</summary>
    public Task<HotSwapResult> SwapAsync(string stepId, ExecutionMode targetMode, CancellationToken ct = default)
    {
        var previous = GetMode(stepId);
        SetModeAsync(stepId, targetMode, ct).GetAwaiter().GetResult();
        _logger?.LogInformation("Hot-swap: {StepId} {Previous} -> {New}", stepId, previous, targetMode);
        return Task.FromResult(new HotSwapResult(stepId, previous, targetMode, true, null, DateTimeOffset.UtcNow));
    }

    private void Load()
    {
        if (!File.Exists(_configPath)) return;
        try
        {
            var json = File.ReadAllText(_configPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null)
            {
                lock (_lock)
                {
                    foreach (var kv in dict)
                    {
                        if (Enum.TryParse<ExecutionMode>(kv.Value, ignoreCase: true, out var mode))
                            _modes[kv.Key] = mode;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load step modes from {Path}", _configPath);
        }
    }

    private void Save()
    {
        try
        {
            Dictionary<string, string> dict;
            lock (_lock)
            {
                dict = _modes.ToDictionary(k => k.Key, v => v.Value.ToString());
            }
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(_configPath, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save step modes to {Path}", _configPath);
        }
    }
}
