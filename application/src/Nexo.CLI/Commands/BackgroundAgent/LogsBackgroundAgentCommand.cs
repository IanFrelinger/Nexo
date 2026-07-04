using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexo.BackgroundAgents.Logging;

namespace Nexo.CLI.Commands.BackgroundAgent;

/// <summary>
/// CLI command to show agent execution logs.
/// </summary>
public class LogsBackgroundAgentCommand
{
    private readonly IBackgroundAgentLogStore? _logStore;
    private readonly ILogger<LogsBackgroundAgentCommand> _logger;

    /// <summary>Creates a new LogsBackgroundAgentCommand instance.</summary>
    public LogsBackgroundAgentCommand(
        ILogger<LogsBackgroundAgentCommand> logger,
        IBackgroundAgentLogStore? logStore = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logStore = logStore;
    }

    /// <summary>
    /// Show recent log entries for an agent.
    /// </summary>
    public Task<int> ExecuteAsync(string id, int tail, string? level, TimeSpan? since, bool formatJson, CancellationToken ct = default)
    {
        try
        {
            DateTimeOffset? sinceUtc = since.HasValue ? DateTimeOffset.UtcNow - since.Value : null;
            var entries = _logStore?.GetRecent(id, tail, level, sinceUtc) ?? Array.Empty<AgentLogEntry>();
            if (formatJson)
            {
                var items = entries.Select(e => new { e.Timestamp, e.Level, e.Message }).ToList();
                var options = new JsonSerializerOptions { WriteIndented = true };
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = true, agentId = id, logs = items }, options));
            }
            else
            {
                if (entries.Count == 0)
                    Console.Out.WriteLine($"No logs for agent: {id}");
                else
                {
                    Console.Out.WriteLine($"Logs for agent: {id}");
                    foreach (var e in entries)
                        Console.Out.WriteLine($"  [{e.Timestamp:O}] {e.Level}: {e.Message}");
                }
            }
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logs command failed");
            if (formatJson)
                Console.Out.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }));
            else
                Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }
}
