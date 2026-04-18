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
    private readonly string? _modelProvider;
    private readonly string? _modelName;

    public string Name { get; }

    public ToolCallingAgent(
        string name,
        IModel model,
        ILogger<ToolCallingAgent>? logger = null,
        string? objective = null,
        string? modelProvider = null,
        string? modelName = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _logger = logger;
        _objective = objective;
        _modelProvider = NormalizeOrNull(modelProvider);
        _modelName = NormalizeOrNull(modelName);
    }

    private static string? NormalizeOrNull(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

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

        var stateJson = JsonSerializer.Serialize(snapshot.Data, new JsonSerializerOptions
        {
            WriteIndented = false
        });
        var toolDescriptions = string.Join("\n", schemas.Select(s =>
            $"- {s.Id}: {s.Description} (args: {s.InputJsonSchema})"));

        // Mirror the RepoRoot value into a hint so the model can copy it verbatim into tool args
        // (it's the most common cause of malformed first-cycle calls).
        snapshot.Data.TryGetValue("RepoRoot", out var repoRootObj);
        var repoRoot = repoRootObj as string ?? ".";

        // Provider/model directives. HotSwappableModel and ProviderBackedModel scan the system
        // prompt for these markers; without them the chain falls through to a deterministic echo
        // and the planner appears to "do nothing" because the LLM is never actually called.
        var directiveLines = new List<string>();
        if (_modelProvider is not null)
            directiveLines.Add($"nexo.model.provider={_modelProvider}");
        if (_modelName is not null)
            directiveLines.Add($"nexo.model.name={_modelName}");
        var directives = directiveLines.Count == 0
            ? string.Empty
            : string.Join('\n', directiveLines) + "\n";

        var systemPrompt = $@"{directives}You are a self-extending code agent operating against a real repository through tools.

World state (JSON): {stateJson}

Available tools:
{toolDescriptions}

Operating rules:
- Use ""{repoRoot}"" as the ""root"" argument for every repo.fs.* call (it matches RepoRoot above).
- When you do not yet know enough to act, START by calling repo.fs.list and/or repo.fs.read to gather context — do NOT return an empty tool_calls array just because the world state looks sparse.
- Reserve an empty tool_calls array for the case when you have inspected the repository and concluded that no action is needed this cycle. In that case also include a ""rationale"" string explaining why.
- Prefer small, reversible steps: list → read → propose a single write or search_replace.
- All write paths must be relative to root and live under one of: src/, tests/, docs/, .nexo/.

Response format: a single JSON object, no markdown, with this shape:
{{""tool_calls"": [{{""id"": ""<tool>"", ""arguments"": {{...}}}}], ""rationale"": ""<short reason>""}}";

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
            var (toolCalls, rationale) = ParseResponse(text);
            if (toolCalls.Count == 0)
            {
                // Truncated raw text helps diagnose whether the model returned the wrong shape,
                // wrapped JSON in prose, or genuinely chose to no-op. Capped to keep logs bounded.
                var preview = text.Length > 600 ? text[..600] + "…" : text;
                if (!string.IsNullOrWhiteSpace(rationale))
                    _logger?.LogInformation(
                        "Agent {Agent} returned no tool calls. Rationale: {Rationale}. Raw: {Raw}",
                        Name, rationale, preview);
                else
                    _logger?.LogInformation(
                        "Agent {Agent} returned no tool calls (no rationale provided). Raw: {Raw}",
                        Name, preview);
                return AgentActions.None;
            }
            if (!string.IsNullOrWhiteSpace(rationale))
                _logger?.LogInformation("Agent {Agent} proposed {Count} tool call(s). Rationale: {Rationale}", Name, toolCalls.Count, rationale);
            else
                _logger?.LogInformation("Agent {Agent} proposed {Count} tool call(s).", Name, toolCalls.Count);
            return new AgentActions(toolCalls);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "ThinkAsync failed; returning no actions.");
            mem.Write(new EventRecord(DateTimeOffset.UtcNow, Name, "think.error", ex.Message));
            return AgentActions.None;
        }
    }

    private static (List<ToolCall> Calls, string? Rationale) ParseResponse(string text)
    {
        var result = new List<ToolCall>();
        string? rationale = null;
        var json = ExtractJson(text);
        if (string.IsNullOrWhiteSpace(json))
            return (result, rationale);

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("rationale", out var rEl) && rEl.ValueKind == JsonValueKind.String)
                rationale = rEl.GetString();
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
            // Return what we have on parse error.
        }

        return (result, rationale);
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
