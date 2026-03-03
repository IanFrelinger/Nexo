using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.Metrics;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// CLI command to show agent performance metrics.
/// </summary>
public class MetricsBackgroundAgentCommand
{
    private readonly IBackgroundAgentRegistry _registry;
    private readonly IAggressivenessModeStore _modeStore;
    private readonly ILogger<MetricsBackgroundAgentCommand> _logger;

    public MetricsBackgroundAgentCommand(
        IBackgroundAgentRegistry registry,
        IAggressivenessModeStore modeStore,
        ILogger<MetricsBackgroundAgentCommand> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _modeStore = modeStore ?? throw new ArgumentNullException(nameof(modeStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Show metrics for an agent (execution count, success rate, last times, last error).
    /// </summary>
    public Task<int> ExecuteAsync(string id, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            var instance = _registry.GetAgent(id);
            if (instance == null)
            {
                if (formatJson)
                    Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = $"Agent '{id}' not found" }));
                else
                    Console.Error.WriteLine($"Agent '{id}' not found");
                return Task.FromResult(1);
            }
            var snapshot = ToSnapshot(instance, _modeStore);
            if (formatJson)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, agentId = id, metrics = snapshot }, options));
            }
            else
            {
                Console.Out.WriteLine($"Agent Metrics: {id}");
                Console.Out.WriteLine($"  Mode: {ToModeString(snapshot.Mode)}");
                Console.Out.WriteLine($"  Execution Count: {snapshot.ExecutionCount}");
                Console.Out.WriteLine($"  Success Count: {snapshot.SuccessCount}");
                Console.Out.WriteLine($"  Failure Count: {snapshot.FailureCount}");
                if (snapshot.SuccessRate.HasValue)
                    Console.Out.WriteLine($"  Success Rate: {snapshot.SuccessRate.Value:P1}");
                Console.Out.WriteLine($"  Last Started: {snapshot.LastStartedAt?.ToString("O") ?? "-"}");
                Console.Out.WriteLine($"  Last Completed: {snapshot.LastCompletedAt?.ToString("O") ?? "-"}");
                if (!string.IsNullOrEmpty(snapshot.LastError))
                    Console.Out.WriteLine($"  Last Error: {snapshot.LastError}");
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metrics command failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    private static AgentMetricsSnapshot ToSnapshot(BackgroundAgentInstance instance, IAggressivenessModeStore modeStore)
    {
        var exec = instance.ExecutionCount;
        var successRate = exec > 0 ? (double)instance.SuccessCount / exec : (double?)null;
        return new AgentMetricsSnapshot(
            instance.Config.Id,
            instance.ExecutionCount,
            instance.SuccessCount,
            instance.FailureCount,
            successRate,
            instance.LastStartedAt,
            instance.LastCompletedAt,
            instance.LastError,
            modeStore.GetMode());
    }

    private static string ToModeString(Nexo.BackgroundAgents.Configuration.BackgroundAgentAggressivenessMode mode)
    {
        return mode switch
        {
            Nexo.BackgroundAgents.Configuration.BackgroundAgentAggressivenessMode.Passive => "passive",
            Nexo.BackgroundAgents.Configuration.BackgroundAgentAggressivenessMode.SemiActive => "semi-active",
            Nexo.BackgroundAgents.Configuration.BackgroundAgentAggressivenessMode.Active => "active",
            Nexo.BackgroundAgents.Configuration.BackgroundAgentAggressivenessMode.Ambient => "ambient",
            _ => mode.ToString().ToLowerInvariant()
        };
    }
}
