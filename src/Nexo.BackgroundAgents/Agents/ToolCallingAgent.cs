using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Nexo.Abstractions;

namespace Nexo.BackgroundAgents.Agents;

/// <summary>
/// IAgent that uses an LLM to produce tool calls.
/// Builds a prompt with tool schemas and world state, calls IModel.CompleteAsync,
/// parses the response for a JSON object with tool_calls, and returns AgentActions.
/// Used by the self-extend runner so background agents can modify the codebase within policy guardrails.
/// </summary>
public sealed class ToolCallingAgent : IAgent
{
    private readonly IModel _model;
    private readonly ILogger<ToolCallingAgent>? _logger;
    private readonly string? _objective;

    public string Name { get; }

    public ToolCallingAgent(string name, IModel model, ILogger<ToolCallingAgent>? logger = null, string? objective = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _logger = logger;
        _objective = objective;
    }

    /// <inheritdoc />
    public async Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct)
    {
        var snapshot = obs.Snapshot;
        var schemas = tools.Schemas().ToList();
        if (schemas.Count == 0)
        {
            _logger?.LogDebug("No tools available; returning no actions.");
            return AgentActions.None;
        }

        var stateJson = JsonSerializer.Serialize(snapshot.Data);
        var toolDescriptions = string.Join("\n", schemas.Select(s =>
            $"- {s.Id}: {s.Description} (args: {s.InputJsonSchema})"));

        var systemPrompt = $@"You are a self-extending code agent. You may call tools to read/write files in the repository.
Current world state (JSON): {stateJson}

Available tools:
{toolDescriptions}

Respond with a single JSON object with a property ""tool_calls"" (array). Each element has ""id"" (tool name) and ""arguments"" (object). If you have nothing to do, respond with empty tool_calls array. JSON only, no markdown.";

        var messages = new List<(string role, string content)>
        {
            ("system", systemPrompt),
            ("user", BuildUserPrompt(_objective))
        };

        try
        {
            var input = new ModelInput(messages);
            var output = await _model.CompleteAsync(input, ct).ConfigureAwait(false);
            var text = output?.Text?.Trim() ?? "";
            var toolCalls = ParseToolCalls(text);
            if (toolCalls.Count == 0)
            {
                _logger?.LogDebug("Model returned no tool calls.");
                return AgentActions.None;
            }
            _logger?.LogDebug("Model returned {Count} tool call(s).", toolCalls.Count);
            return new AgentActions(toolCalls);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "ThinkAsync failed; returning no actions.");
            mem.Write(new EventRecord(DateTimeOffset.UtcNow, Name, "think.error", ex.Message));
            return AgentActions.None;
        }
    }

    private static List<ToolCall> ParseToolCalls(string text)
    {
        var result = new List<ToolCall>();
        var json = ExtractJson(text);
        if (string.IsNullOrWhiteSpace(json))
            return result;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("tool_calls", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idEl) && item.TryGetProperty("arguments", out var argsEl))
                    {
                        var id = idEl.GetString();
                        if (string.IsNullOrWhiteSpace(id))
                            continue;
                        var argsJson = argsEl.GetRawText();
                        var argsCopy = JsonSerializer.Deserialize<JsonElement>(argsJson);
                        result.Add(new ToolCall(id, argsCopy));
                    }
                }
            }
        }
        catch
        {
            // Return empty on parse error
        }

        return result;
    }

    private static string? ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        text = text.Trim();
        var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value.Trim();
        if (text.StartsWith("{", StringComparison.Ordinal))
            return text;
        return null;
    }

    private static string BuildUserPrompt(string? objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return "Decide which tools to call based on the current state. Respond with JSON only.";

        return $"Objective:\n{objective.Trim()}\n\nUse available tools to complete the objective. Respond with JSON only.";
    }
}
