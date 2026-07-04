using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.DataSensitivity;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// CLI command handler for data sensitivity level operations (list, show, add, update, remove).
/// </summary>
public class SensitivityCommand
{
    private readonly IDataSensitivityRegistry _registry;
    private readonly ILogger<SensitivityCommand> _logger;

    /// <summary>Creates the sensitivity analysis command.</summary>
    public SensitivityCommand(
        IDataSensitivityRegistry registry,
        ILogger<SensitivityCommand> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Creates a new ListAsync instance.</summary>
    public Task<int> ListAsync(bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var levels = _registry.GetAll();
            if (formatJson)
            {
                var items = levels.Select(l => new
                {
                    Name = l.Value,
                    Display = l.Display,
                    SensitivityValue = l.SensitivityValue,
                    AllowsExternalLLM = l.AllowsExternalLLM,
                    AllowsWebSearch = l.AllowsWebSearch,
                    RequiresLocalOnly = l.RequiresLocalOnly,
                    AllowsNetworkExports = l.AllowsNetworkExports,
                    Description = l.Description
                }).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(items, options));
            }
            else
            {
                Console.Out.WriteLine("Sensitivity Levels:");
                foreach (var l in levels)
                    Console.Out.WriteLine($"  {l.Value} (value={l.SensitivityValue}) - {l.Description}");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List sensitivity levels failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>Creates a new ShowAsync instance.</summary>
    public Task<int> ShowAsync(string name, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var level = _registry.GetByName(name);
            if (level == null)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"Sensitivity level '{name}' not found" }));
                else
                    Console.Error.WriteLine($"Sensitivity level '{name}' not found");
                return Task.FromResult(1);
            }
            if (formatJson)
            {
                var payload = new
                {
                    level.Value,
                    level.Display,
                    level.SensitivityValue,
                    level.AllowsExternalLLM,
                    level.AllowsWebSearch,
                    level.RequiresLocalOnly,
                    level.AllowsNetworkExports,
                    level.Description
                };
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(payload, options));
            }
            else
            {
                Console.Out.WriteLine($"Sensitivity: {level.Value}");
                Console.Out.WriteLine($"  Value: {level.SensitivityValue}");
                Console.Out.WriteLine($"  Description: {level.Description}");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Show sensitivity level failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>Creates a new AddAsync instance.</summary>
    public Task<int> AddAsync(string name, int sensitivityValue, bool allowsExternalLLM, bool allowsWebSearch, bool requiresLocalOnly, bool allowsNetworkExports, string description, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var level = new ConfigurableSensitivityLevel(name, name, sensitivityValue, allowsExternalLLM, allowsWebSearch, requiresLocalOnly, allowsNetworkExports, description ?? string.Empty);
            _registry.Register(level);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, name, action = "added" }));
            else
                Console.Out.WriteLine($"Added sensitivity level: {name}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add sensitivity level failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>Creates a new UpdateAsync instance.</summary>
    public Task<int> UpdateAsync(string name, int sensitivityValue, bool allowsExternalLLM, bool allowsWebSearch, bool requiresLocalOnly, bool allowsNetworkExports, string description, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            if (_registry.GetByName(name) == null)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"Sensitivity level '{name}' not found" }));
                else
                    Console.Error.WriteLine($"Sensitivity level '{name}' not found");
                return Task.FromResult(1);
            }
            if (DataSensitivityLevels.FromName(name) != null)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "Cannot update primitive sensitivity level" }));
                else
                    Console.Error.WriteLine("Cannot update primitive sensitivity level");
                return Task.FromResult(1);
            }
            _registry.Unregister(name);
            var level = new ConfigurableSensitivityLevel(name, name, sensitivityValue, allowsExternalLLM, allowsWebSearch, requiresLocalOnly, allowsNetworkExports, description ?? string.Empty);
            _registry.Register(level);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, name, action = "updated" }));
            else
                Console.Out.WriteLine($"Updated sensitivity level: {name}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update sensitivity level failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    /// <summary>Creates a new RemoveAsync instance.</summary>
    public Task<int> RemoveAsync(string name, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var removed = _registry.Unregister(name);
            if (!removed)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"Sensitivity level '{name}' not found or is primitive" }));
                else
                    Console.Error.WriteLine($"Sensitivity level '{name}' not found or cannot remove primitive level");
                return Task.FromResult(1);
            }
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, name, action = "removed" }));
            else
                Console.Out.WriteLine($"Removed sensitivity level: {name}");
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Remove sensitivity level failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }
}
