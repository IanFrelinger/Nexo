using System.Collections.Generic;
using Nexo.Abstractions;
using Nexo.BackgroundAgents.Configuration;
using Nexo.BackgroundAgents.DataSensitivity;
using Nexo.BackgroundAgents.Registry;

namespace Nexo.BackgroundAgents.Security;

/// <summary>
/// Policy that enforces exfiltration prevention for background agent tool calls.
///
/// Uses agent id from WorldSnapshot.Data["agentId"] to look up BackgroundAgentConfig,
/// then applies ExfiltrationPolicy to block tool calls that would send data to
/// external LLMs, web search, or network exports when disallowed.
/// Implements IPolicy for use with PolicyEngine.
/// </summary>
public sealed class DataExfiltrationPolicy : IPolicy
{
    private static readonly HashSet<string> DefaultExternalLlmToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "complete", "chat", "llm_complete", "LlmComplete", "openai_complete", "ollama_complete"
    };

    private static readonly HashSet<string> DefaultWebSearchToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "web_search", "WebSearch", "search", "Search", "bing_search", "duckduckgo_search"
    };

    private static readonly HashSet<string> DefaultNetworkExportToolIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "export", "Export", "upload", "Upload", "send_to_network", "SendToNetwork", "http_post", "HttpPost"
    };

    private readonly IBackgroundAgentRegistry _registry;
    private readonly IDataSensitivityRegistry _sensitivityRegistry;
    private readonly IReadOnlySet<string> _externalLlmToolIds;
    private readonly IReadOnlySet<string> _webSearchToolIds;
    private readonly IReadOnlySet<string> _networkExportToolIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataExfiltrationPolicy"/> class.
    /// </summary>
    /// <param name="registry">Background agent registry to resolve agent config from snapshot.</param>
    /// <param name="sensitivityRegistry">Sensitivity registry to resolve MaxAllowedLevel.</param>
    /// <param name="externalLlmToolIds">Tool ids treated as external LLM (optional; uses defaults if null).</param>
    /// <param name="webSearchToolIds">Tool ids treated as web search (optional; uses defaults if null).</param>
    /// <param name="networkExportToolIds">Tool ids treated as network export (optional; uses defaults if null).</param>
    public DataExfiltrationPolicy(
        IBackgroundAgentRegistry registry,
        IDataSensitivityRegistry sensitivityRegistry,
        IEnumerable<string>? externalLlmToolIds = null,
        IEnumerable<string>? webSearchToolIds = null,
        IEnumerable<string>? networkExportToolIds = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _sensitivityRegistry = sensitivityRegistry ?? throw new ArgumentNullException(nameof(sensitivityRegistry));
        _externalLlmToolIds = externalLlmToolIds != null
            ? new HashSet<string>(externalLlmToolIds, StringComparer.OrdinalIgnoreCase)
            : DefaultExternalLlmToolIds;
        _webSearchToolIds = webSearchToolIds != null
            ? new HashSet<string>(webSearchToolIds, StringComparer.OrdinalIgnoreCase)
            : DefaultWebSearchToolIds;
        _networkExportToolIds = networkExportToolIds != null
            ? new HashSet<string>(networkExportToolIds, StringComparer.OrdinalIgnoreCase)
            : DefaultNetworkExportToolIds;
    }

    /// <inheritdoc />
    public bool Approve(ToolCall call, WorldSnapshot s, out string reason)
    {
        reason = string.Empty;

        if (!s.Data.TryGetValue("agentId", out var agentIdObj) || agentIdObj is not string agentId)
        {
            reason = "OK";
            return true;
        }

        var instance = _registry.GetAgent(agentId);
        if (instance == null)
        {
            reason = "OK";
            return true;
        }

        var policy = instance.Config.ExfiltrationPolicy;
        var toolId = call.Id ?? string.Empty;

        if (policy.BlockExternalLLMs && _externalLlmToolIds.Contains(toolId))
        {
            reason = $"Exfiltration policy blocks external LLM tool: {toolId}";
            return false;
        }

        if (policy.BlockWebSearch && _webSearchToolIds.Contains(toolId))
        {
            reason = $"Exfiltration policy blocks web search tool: {toolId}";
            return false;
        }

        if (policy.BlockNetworkExports && _networkExportToolIds.Contains(toolId))
        {
            reason = $"Exfiltration policy blocks network export tool: {toolId}";
            return false;
        }

        if (policy.RequireLocalOnly)
        {
            if (_externalLlmToolIds.Contains(toolId) || _webSearchToolIds.Contains(toolId) || _networkExportToolIds.Contains(toolId))
            {
                reason = $"Exfiltration policy requires local-only; tool {toolId} is not local";
                return false;
            }
        }

        reason = "OK";
        return true;
    }
}
