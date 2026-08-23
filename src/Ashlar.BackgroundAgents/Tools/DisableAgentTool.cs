using System.Text.Json;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Registry;

namespace Ashlar.BackgroundAgents.Tools;

/// <summary>
/// ITool that stops (disables) a background agent. Id: "disable_agent".
/// </summary>
public sealed class DisableAgentTool : ITool
{
    /// <summary>Tool id.</summary>
    public const string DefaultId = "disable_agent";

    private static readonly ToolSchema SchemaInstance = new(
        DefaultId,
        "Stop a background agent by ID.",
        """{"type":"object","properties":{"agentId":{"type":"string","description":"Background agent ID to stop"}},"required":["agentId"]}""");

    private readonly IBackgroundAgentRegistry _registry;

    /// <inheritdoc />
    public string Id => DefaultId;

    /// <inheritdoc />
    public ToolSchema Schema => SchemaInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisableAgentTool"/> class.
    /// </summary>
    /// <param name="registry">Background agent registry.</param>
    public DisableAgentTool(IBackgroundAgentRegistry registry)
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
            var err = new ActionDelta(s.Tick, s.Tick + 1, new[] { "disable_agent: agentId is required" });
            return new ToolResult(err, new { ok = false, error = "agentId is required" });
        }
        try
        {
            await _registry.StopAsync(agentId, ct).ConfigureAwait(false);
            var log = new[] { $"Stopped background agent: {agentId}" };
            var delta = new ActionDelta(s.Tick, s.Tick + 1, log);
            return new ToolResult(delta, new { ok = true, agentId, action = "stopped" });
        }
        catch (Exception ex)
        {
            var log = new[] { $"disable_agent failed: {ex.Message}" };
            var delta = new ActionDelta(s.Tick, s.Tick + 1, log);
            return new ToolResult(delta, new { ok = false, agentId, error = ex.Message });
        }
    }

    private static DisableAgentArgs ParseArgs(ToolCall call)
    {
        try
        {
            var json = call.Arguments.GetRawText();
            return JsonSerializer.Deserialize<DisableAgentArgs>(json) ?? new DisableAgentArgs();
        }
        catch
        {
            return new DisableAgentArgs();
        }
    }

    private sealed class DisableAgentArgs
    {
        [System.Text.Json.Serialization.JsonPropertyName("agentId")]
        public string? AgentId { get; set; }
    }
}
