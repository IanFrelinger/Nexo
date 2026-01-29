using System.Text.Json;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.BackgroundAgents.Tools;

/// <summary>
/// ITool that starts (enables) a background agent. Id: "enable_agent".
/// </summary>
public sealed class EnableAgentTool : ITool
{
    /// <summary>Tool id.</summary>
    public const string DefaultId = "enable_agent";

    private static readonly ToolSchema SchemaInstance = new(
        DefaultId,
        "Start a background agent by ID. The agent must be registered (e.g. from configuration) before it can be started.",
        """{"type":"object","properties":{"agentId":{"type":"string","description":"Background agent ID to start"}},"required":["agentId"]}""");

    private readonly IBackgroundAgentRegistry _registry;

    /// <inheritdoc />
    public string Id => DefaultId;

    /// <inheritdoc />
    public ToolSchema Schema => SchemaInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnableAgentTool"/> class.
    /// </summary>
    /// <param name="registry">Background agent registry.</param>
    public EnableAgentTool(IBackgroundAgentRegistry registry)
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
            var err = new ActionDelta(s.Tick, s.Tick + 1, new[] { "enable_agent: agentId is required" });
            return new ToolResult(err, new { ok = false, error = "agentId is required" });
        }
        try
        {
            await _registry.StartAsync(agentId, ct).ConfigureAwait(false);
            var log = new[] { $"Started background agent: {agentId}" };
            var delta = new ActionDelta(s.Tick, s.Tick + 1, log);
            return new ToolResult(delta, new { ok = true, agentId, action = "started" });
        }
        catch (Exception ex)
        {
            var log = new[] { $"enable_agent failed: {ex.Message}" };
            var delta = new ActionDelta(s.Tick, s.Tick + 1, log);
            return new ToolResult(delta, new { ok = false, agentId, error = ex.Message });
        }
    }

    private static EnableAgentArgs ParseArgs(ToolCall call)
    {
        try
        {
            var json = call.Arguments.GetRawText();
            return JsonSerializer.Deserialize<EnableAgentArgs>(json) ?? new EnableAgentArgs();
        }
        catch
        {
            return new EnableAgentArgs();
        }
    }

    private sealed class EnableAgentArgs
    {
        [System.Text.Json.Serialization.JsonPropertyName("agentId")]
        public string? AgentId { get; set; }
    }
}
