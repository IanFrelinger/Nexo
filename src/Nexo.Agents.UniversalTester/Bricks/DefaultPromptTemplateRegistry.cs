using System.Text.Json;
using Nexo.Infrastructure.IO;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Built-in prompt templates for Understanding and other bricks. Can be extended by loading from file.
/// </summary>
public sealed class DefaultPromptTemplateRegistry : IPromptTemplateRegistry
{
    private readonly Dictionary<string, PromptTemplate> _templates;
    private static readonly IReadOnlyDictionary<string, PromptTemplate> BuiltIn = BuildBuiltIn();

    /// <summary>Creates a registry with built-in templates only.</summary>
    public DefaultPromptTemplateRegistry()
    {
        _templates = new Dictionary<string, PromptTemplate>(BuiltIn, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Creates a registry with built-in templates plus additional/override templates.</summary>
    public DefaultPromptTemplateRegistry(IEnumerable<KeyValuePair<string, PromptTemplate>> additional)
    {
        _templates = new Dictionary<string, PromptTemplate>(BuiltIn, StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in additional)
            _templates[k] = v;
    }

    /// <inheritdoc />
    public PromptTemplate? TryGetTemplate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _templates.TryGetValue(name.Trim(), out var t) ? t : null;
    }

    /// <summary>
    /// Creates a registry, optionally loading additional templates from a JSON file.
    /// File format: { "templateName": { "systemPrompt": "...", "userPromptTemplate": "..." }, ... }
    /// </summary>
    public static DefaultPromptTemplateRegistry FromPath(string? path)
    {
        var reg = new DefaultPromptTemplateRegistry();
        if (string.IsNullOrWhiteSpace(path)) return reg;
        try
        {
            var json = TextFile.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict == null) return reg;
            foreach (var (k, v) in dict)
            {
                if (string.IsNullOrWhiteSpace(k)) continue;
                var sys = v.TryGetProperty("systemPrompt", out var sp) ? sp.GetString() : null;
                var user = v.TryGetProperty("userPromptTemplate", out var up) ? up.GetString() : null;
                reg._templates[k.Trim()] = new PromptTemplate { SystemPrompt = sys, UserPromptTemplate = user };
            }
        }
        catch
        {
            // Silently fall back to built-in only; callers can log if needed
        }
        return reg;
    }

    /// <summary>Builds the built-in templates (temporal-gameplay, understanding-vision-default).</summary>
    private static IReadOnlyDictionary<string, PromptTemplate> BuildBuiltIn()
    {
        return new Dictionary<string, PromptTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["temporal-gameplay"] = new()
            {
                SystemPrompt = "You are analyzing video game gameplay. Summarize what is happening: game state, player actions, combat, UI, notable events. Put the summary in currentContext. Return only valid JSON with screenType, currentContext (your summary), availableActions (empty array), currentObjective, progressPercent, issues, unexploredAreas, confidence.",
                UserPromptTemplate = @"These {{frameCount}} images are consecutive video frames over time (oldest first). Describe the gameplay flow, changes, and notable moments.
{{gameStateSection}}
{{audioSection}}
## Testing Goal:
{{goal}}
{{constraintsSection}}
Return JSON only (no markdown):
{
  ""screenType"": ""e.g. Action game combat, RPG menu, platformer level"",
  ""currentContext"": ""Your live summary: what is happening, game state, player actions, notable events"",
  ""availableActions"": [],
  ""currentObjective"": ""Observation/summary"",
  ""progressPercent"": 0,
  ""issues"": [],
  ""unexploredAreas"": [],
  ""confidence"": 0.8
}"
            },
            ["understanding-vision-default"] = new()
            {
                SystemPrompt = "You are simulating a human user looking at their screen. Describe what you see: windows, buttons, text fields, labels, menus. The app is intended for humans, so identify where a human would click or type. For each action, provide target as screen pixel coordinates \"x,y\" (origin top-left). Return only valid JSON.",
                UserPromptTemplate = @"{{multiFrameIntro}}
{{gameStateSection}}
{{audioSection}}
## Testing Goal:
{{goal}}
{{constraintsSection}}
{{firstTimeUserHint}}
Look at the screenshot as a human would. Identify: buttons (e.g. Send, Chat, Architecture), text input areas, labels. For each action a user could take, give the approximate pixel coordinates of where to click (target ""x,y"", origin top-left of screen). For typing, give the coordinates of the input field and ""value"": ""text to type"".
Return JSON only (no markdown):
{
  ""screenType"": ""e.g. Chat app with sidebar"",
  ""currentContext"": ""What is visible and what a user would see"",
  ""availableActions"": [
    { ""id"": ""click_send"", ""description"": ""Click the Send button"", ""type"": ""click"", ""target"": ""x,y"", ""relevanceToGoal"": 0.9, ""explorationValue"": 0.5, ""riskLevel"": 0.1 },
    { ""id"": ""type_chat"", ""description"": ""Type in the chat input"", ""type"": ""type"", ""target"": ""x,y"", ""value"": ""hello"", ""relevanceToGoal"": 0.8, ""explorationValue"": 0.6, ""riskLevel"": 0.1 }
  ],
  ""currentObjective"": ""..."",
  ""progressPercent"": 0,
  ""issues"": [],
  ""unexploredAreas"": [],
  ""confidence"": 0.8
}"
            }
        };
    }
}
