using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// CLI command handler for background agent operations (list, show, start, stop, restart).
/// Uses config loader for list/show; uses registry for start/stop/restart after ensuring agents are registered.
/// </summary>
public class BackgroundAgentCommand
{
    private readonly BackgroundAgentConfigLoader _configLoader;
    private readonly IBackgroundAgentRegistry _registry;
    private readonly BackgroundAgentSpecBuilder _specBuilder;
    private readonly Nexo.Orchestration.Agents.AgentFactory _agentFactory;
    private readonly ILogger<BackgroundAgentCommand> _logger;

    public BackgroundAgentCommand(
        BackgroundAgentConfigLoader configLoader,
        IBackgroundAgentRegistry registry,
        BackgroundAgentSpecBuilder specBuilder,
        Nexo.Orchestration.Agents.AgentFactory agentFactory,
        ILogger<BackgroundAgentCommand> logger)
    {
        _configLoader = configLoader ?? throw new ArgumentNullException(nameof(configLoader));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _specBuilder = specBuilder ?? throw new ArgumentNullException(nameof(specBuilder));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// List configured agents (from config); optionally merge state from registry.
    /// </summary>
    public async Task<int> ListAsync(bool formatJson, string? status, string? role, string? sensitivity, CancellationToken ct = default)
    {
        try
        {
            var configs = await _configLoader.LoadAsync(ct);
            var instances = _registry.GetAll();
            var byId = instances.ToDictionary(i => i.Config.Id, StringComparer.OrdinalIgnoreCase);

            var items = configs
                .Where(c => FilterByStatus(c, byId, status))
                .Where(c => FilterByRole(c, role))
                .Where(c => FilterBySensitivity(c, sensitivity))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Role,
                    State = byId.TryGetValue(c.Id, out var inst) ? inst.State.ToString() : "NotRegistered",
                    MaxSensitivity = c.MaxDataSensitivity
                })
                .ToList();

            if (formatJson)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(items, options));
            }
            else
            {
                Console.Out.WriteLine("Background Agents:");
                foreach (var item in items)
                    Console.Out.WriteLine($"  {item.Id} ({item.Role}) - {item.State} - Max Sensitivity: {item.MaxSensitivity}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List background agents failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Show details for one agent by id.
    /// </summary>
    public async Task<int> ShowAsync(string id, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var configs = await _configLoader.LoadAsync(ct);
            var config = configs.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
            if (config == null)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"Agent '{id}' not found" }));
                else
                    Console.Error.WriteLine($"Agent '{id}' not found");
                return 1;
            }

            var instance = _registry.GetAgent(config.Id);

            if (formatJson)
            {
                var payload = new
                {
                    config.Id,
                    config.Name,
                    config.Role,
                    State = instance?.State.ToString() ?? "NotRegistered",
                    config.ModelProvider,
                    config.ModelName,
                    config.MaxDataSensitivity,
                    Schedule = new { config.Schedule.Type, config.Schedule.Interval, config.Schedule.CronExpression },
                    config.Commands,
                    RAG = config.RAG?.Enabled ?? false,
                    WebSearch = config.WebSearch?.Enabled ?? false,
                    ExfiltrationPolicy = new
                    {
                        config.ExfiltrationPolicy.BlockExternalLLMs,
                        config.ExfiltrationPolicy.BlockWebSearch,
                        config.ExfiltrationPolicy.BlockNetworkExports,
                        config.ExfiltrationPolicy.MaxAllowedLevel
                    },
                    instance?.LastStartedAt,
                    instance?.LastCompletedAt,
                    instance?.ExecutionCount,
                    instance?.SuccessCount,
                    instance?.FailureCount,
                    instance?.LastError
                };
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(payload, options));
            }
            else
            {
                Console.Out.WriteLine($"Agent: {config.Id}");
                Console.Out.WriteLine($"  Name: {config.Name}");
                Console.Out.WriteLine($"  Role: {config.Role}");
                Console.Out.WriteLine($"  Status: {instance?.State.ToString() ?? "NotRegistered"}");
                Console.Out.WriteLine($"  Model Provider: {config.ModelProvider}");
                Console.Out.WriteLine($"  Max Data Sensitivity: {config.MaxDataSensitivity}");
                Console.Out.WriteLine($"  Schedule: {config.Schedule.Type} {config.Schedule.Interval} {config.Schedule.CronExpression}");
                Console.Out.WriteLine($"  Commands: {string.Join(", ", config.Commands)}");
                Console.Out.WriteLine($"  RAG: {(config.RAG?.Enabled == true ? "Enabled" : "Disabled")}");
                Console.Out.WriteLine($"  Web Search: {(config.WebSearch?.Enabled == true ? "Enabled" : "Disabled")}");
                Console.Out.WriteLine("  Exfiltration Policy:");
                Console.Out.WriteLine($"    - Block External LLMs: {config.ExfiltrationPolicy.BlockExternalLLMs}");
                Console.Out.WriteLine($"    - Block Web Search: {config.ExfiltrationPolicy.BlockWebSearch}");
                Console.Out.WriteLine($"    - Block Network Exports: {config.ExfiltrationPolicy.BlockNetworkExports}");
                Console.Out.WriteLine($"    - Max Allowed Level: {config.ExfiltrationPolicy.MaxAllowedLevel}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Show background agent failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Ensure agent is registered (load config, create and register if missing), then start.
    /// </summary>
    public async Task<int> StartAsync(string id, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            await EnsureAgentRegisteredAsync(id, ct);
            await _registry.StartAsync(id, ct);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, id, action = "started" }));
            else
                Console.Out.WriteLine($"Started background agent: {id}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start background agent failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Stop an agent by id.
    /// </summary>
    public async Task<int> StopAsync(string id, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            await _registry.StopAsync(id, ct);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, id, action = "stopped" }));
            else
                Console.Out.WriteLine($"Stopped background agent: {id}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stop background agent failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    /// <summary>
    /// Stop then start an agent by id.
    /// </summary>
    public async Task<int> RestartAsync(string id, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            await _registry.StopAsync(id, ct);
            await EnsureAgentRegisteredAsync(id, ct);
            await _registry.StartAsync(id, ct);
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, id, action = "restarted" }));
            else
                Console.Out.WriteLine($"Restarted background agent: {id}");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restart background agent failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private async Task EnsureAgentRegisteredAsync(string id, CancellationToken ct)
    {
        if (_registry.GetAgent(id) != null)
            return;
        var configs = await _configLoader.LoadAsync(ct);
        var config = configs.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Agent '{id}' not found in configuration");
        var spec = _specBuilder.BuildSpec(config);
        var agent = _agentFactory.CreateAgent(spec);
        await _registry.RegisterAsync(agent, config, ct);
    }

    private static bool FilterByStatus(BackgroundAgentConfig c, Dictionary<string, BackgroundAgentInstance> byId, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return true;
        var state = byId.TryGetValue(c.Id, out var inst) ? inst.State.ToString() : "NotRegistered";
        return string.Equals(state, status, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FilterByRole(BackgroundAgentConfig c, string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return true;
        return string.Equals(c.Role, role, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FilterBySensitivity(BackgroundAgentConfig c, string? sensitivity)
    {
        if (string.IsNullOrWhiteSpace(sensitivity))
            return true;
        return string.Equals(c.MaxDataSensitivity, sensitivity, StringComparison.OrdinalIgnoreCase);
    }
}
