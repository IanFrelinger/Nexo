using System.Text.Json;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.BackgroundAgents.Tools;

/// <summary>
/// ITool that restarts a background agent (stop then start). Id: "restart_agent".
/// </summary>
public sealed class RestartAgentTool : ITool
{
    /// <summary>Tool id.</summary>
    public const string DefaultId = "restart_agent";

    private static readonly ToolSchema SchemaInstance = new(
        DefaultId,
        "Restart a background agent by ID (stop then start).",
        """{"type":"object","properties":{"agentId":{"type":"string","description":"Background agent ID to restart"}},"required":["agentId"]}""");

    private readonly IBackgroundAgentRegistry _registry;

    /// <inheritdoc />
    public string Id => DefaultId;

    /// <inheritdoc />
    public ToolSchema Schema => SchemaInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="RestartAgentTool"/> class.
    /// </summary>
    /// <param name="registry">Background agent registry.</param>
    public RestartAgentTool(IBackgroundAgentRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public async Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct)
    {
        var args = ParseArgs(toolCall);
        var agentId = args.AgentId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(agentId))
        {
            var err = new ActionDelta(s.Tick, s.Tick + 1, new[] { "restart_agent: agentId is required" });
            return new ToolResult(err, new { ok = false, error = "agentId is required" });
        }
        try
        {
            await _registry.StopAsync(agentId, ct).ConfigureAwait(false);
            await _registry.StartAsync(agentId, ct).ConfigureAwait(false);
            var log = new[] { $"Restarted background agent: {agentId}" };
            var delta = new ActionDelta(s.Tick, s.Tick + 1, log);
            return new ToolResult(delta, new { ok = true, agentId, action = "restarted" });
        }
        catch (Exception ex)
        {
            var log = new[] { $"restart_agent failed: {ex.Message}" };
            var delta = new ActionDelta(s.Tick, s.Tick + 1, log);
            return new ToolResult(delta, new { ok = false, agentId, error = ex.Message });
        }
    }

    private static RestartAgentArgs ParseArgs(ToolCall call)
    {
        try
        {
            var json = call.Arguments.GetRawText();
            return JsonSerializer.Deserialize<RestartAgentArgs>(json) ?? new RestartAgentArgs();
        }
        catch
        {
            return new RestartAgentArgs();
        }
    }

    private sealed class RestartAgentArgs
    {
        [System.Text.Json.Serialization.JsonPropertyName("agentId")]
        public string? AgentId { get; set; }
    }
}
