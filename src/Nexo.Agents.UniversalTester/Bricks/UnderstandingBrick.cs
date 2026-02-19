using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// AI analyzes what it's seeing and what's possible.
/// </summary>
public class UnderstandingBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly IPromptTemplateRegistry? _promptRegistry;
    private readonly ILogger<UnderstandingBrick>? _logger;
    
    public UnderstandingBrick(IProviderFactory providerFactory, ILogger<UnderstandingBrick>? logger = null)
        : this(providerFactory, null, logger) { }
    
    public UnderstandingBrick(IProviderFactory providerFactory, IPromptTemplateRegistry? promptRegistry, ILogger<UnderstandingBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _promptRegistry = promptRegistry;
        _logger = logger;
        
        Id = "universal-tester.understanding";
        Name = "Understanding";
        Version = "1.0.0";
        Icon = "🧠";
        Category = BrickCategory.Analysis;
        Description = "AI analyzes what it's seeing and what's possible";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("perception", "PerceptionState", "Current perception state"),
                new BrickInputDefinition("goal", "string", "Testing goal"),
                new BrickInputDefinition("constraints", "string[]", "Testing constraints", required: false),
                new BrickInputDefinition("actionHistory", "TestAction[]", "Previous actions", required: false),
                new BrickInputDefinition("previousUnderstanding", "Understanding", "Previous understanding", required: false),
                new BrickInputDefinition("recentScreenshots", "IReadOnlyList<byte[]>", "Recent screenshots for multi-frame vision", required: false),
                new BrickInputDefinition("audioTranscript", "string", "Audio transcript to augment vision (e.g. from Whisper)", required: false),
                new BrickInputDefinition("watchMode", "bool", "When true, produce summary/analysis instead of action suggestions", required: false),
                new BrickInputDefinition("understandingMode", "UnderstandingMode", "How to interpret perception: auto, pixelOnly, gameStateFirst, temporal", required: false),
                new BrickInputDefinition("modelOverride", "string", "Per-brick model override (e.g. llava:7b for Ollama)", required: false),
                new BrickInputDefinition("promptTemplate", "string", "Named template from prompt registry (e.g. temporal-gameplay)", required: false)
            ],
            Outputs = [
                new BrickOutputDefinition("understanding", "Understanding", "AI's understanding of the state")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "understanding-deterministic",
                Name = "Heuristic Understanding",
                Description = "Uses simple heuristics from perception to propose actions",
                Executor = "RuleEngineExecutor",
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "< 50ms",
                    Deterministic = true,
                    RequiresNetwork = false,
                    ResourceUsage = ResourceUsage.Low
                }
            },
            Agentic = new AgenticImplementation
            {
                Id = "understanding-agentic",
                Name = "AI Understanding",
                Description = "Uses LLM to understand current state",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are a universal testing agent analyzing an application.",
                    Temperature = 0.3,
                    MaxTokens = 2000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "3-10s",
                    Deterministic = false,
                    RequiresNetwork = true,
                    ResourceUsage = ResourceUsage.High
                }
            }
        };
        
        DefaultImplementation = ImplementationType.Agentic;
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var perception = input.Get<PerceptionState>("perception");
        var goal = input.Get<string>("goal");
        var constraints = input.Get<string[]>("constraints", Array.Empty<string>()) ?? Array.Empty<string>();
        var actionHistory = input.Get<TestAction[]>("actionHistory", Array.Empty<TestAction>()) ?? Array.Empty<TestAction>();
        var recentScreenshots = input.Get<IReadOnlyList<byte[]>>("recentScreenshots", null);
        var audioTranscript = input.Get<string>("audioTranscript", null);
        var watchMode = input.Get<bool>("watchMode", false);
        var understandingMode = input.Get<UnderstandingMode?>("understandingMode", null) ?? UnderstandingMode.Auto;
        var modelOverride = input.Get<string>("modelOverride", null);
        var promptTemplateName = input.Get<string>("promptTemplate", null);

        if (implementation == ImplementationType.Deterministic || context.IsAirGapped)
        {
            var understanding = BuildDeterministicUnderstanding(perception, goal);
            return new BrickOutput
            {
                ["understanding"] = understanding,
                Summary = $"Understood (deterministic): {understanding.ScreenType}, {understanding.AvailableActions.Count} actions available"
            };
        }

        var prompt = BuildUnderstandingPrompt(perception, goal, constraints, actionHistory);
        var useVision = perception.Screenshot != null && perception.Screenshot.Length > 0
            && perception.InteractiveElements.Count == 0;
        string response;

        if (useVision && perception.Screenshot != null)
        {
            var effectiveMode = ResolveEffectiveMode(understandingMode, perception, recentScreenshots?.Count ?? 0);
            var frameCount = recentScreenshots?.Count ?? 0;
            var frames = recentScreenshots is { Count: > 1 }
                ? recentScreenshots
                : new[] { perception.Screenshot };

            string systemPrompt;
            string visionPrompt;
            var customTemplate = !string.IsNullOrWhiteSpace(promptTemplateName) && _promptRegistry != null
                ? _promptRegistry.TryGetTemplate(promptTemplateName)
                : null;

            if (customTemplate != null)
            {
                var vars = BuildTemplateVars(perception, goal, constraints, frameCount, audioTranscript, watchMode, effectiveMode);
                systemPrompt = !string.IsNullOrWhiteSpace(customTemplate.SystemPrompt)
                    ? PromptTemplate.Substitute(customTemplate.SystemPrompt, vars)
                    : (watchMode
                        ? "You are analyzing video game gameplay. Summarize what is happening: game state, player actions, combat, UI, notable events. Put the summary in currentContext. Return only valid JSON with screenType, currentContext (your summary), availableActions (empty array), currentObjective, progressPercent, issues, unexploredAreas, confidence."
                        : "You are simulating a human user looking at their screen. Describe what you see: windows, buttons, text fields, labels, menus. The app is intended for humans, so identify where a human would click or type. For each action, provide target as screen pixel coordinates \"x,y\" (origin top-left). Return only valid JSON.");
                visionPrompt = !string.IsNullOrWhiteSpace(customTemplate.UserPromptTemplate)
                    ? PromptTemplate.Substitute(customTemplate.UserPromptTemplate, vars)
                    : BuildVisionUnderstandingPrompt(perception, goal, constraints, frameCount, audioTranscript, watchMode, effectiveMode);
            }
            else
            {
                visionPrompt = BuildVisionUnderstandingPrompt(perception, goal, constraints, frameCount, audioTranscript, watchMode, effectiveMode);
                systemPrompt = watchMode
                    ? "You are analyzing video game gameplay. Summarize what is happening: game state, player actions, combat, UI, notable events. Put the summary in currentContext. Return only valid JSON with screenType, currentContext (your summary), availableActions (empty array), currentObjective, progressPercent, issues, unexploredAreas, confidence."
                    : "You are simulating a human user looking at their screen. Describe what you see: windows, buttons, text fields, labels, menus. The app is intended for humans, so identify where a human would click or type. For each action, provide target as screen pixel coordinates \"x,y\" (origin top-left). Return only valid JSON.";
            }

            if (frames.Count > 1)
            {
                response = await _providerFactory.ExecuteVisionMultiFrameAsync(
                    context.Provider,
                    systemPrompt,
                    visionPrompt,
                    frames,
                    modelOverride != null ? new { model = modelOverride } : new { },
                    cancellationToken);
            }
            else
            {
                response = await _providerFactory.ExecuteVisionAsync(
                    context.Provider,
                    systemPrompt,
                    visionPrompt,
                    perception.Screenshot,
                    modelOverride != null ? new { model = modelOverride } : new { },
                    cancellationToken);
            }
        }
        else
        {
            response = await _providerFactory.ExecuteLLMAsync(
                context.Provider,
                "You are a universal testing agent analyzing an application.",
                prompt,
                modelOverride != null ? new { model = modelOverride } : new { },
                cancellationToken);
        }

        var parsed = ParseUnderstanding(response, perception);

        return new BrickOutput
        {
            ["understanding"] = parsed,
            Summary = $"Understood: {parsed.ScreenType}, {parsed.AvailableActions.Count} actions available"
        };
    }

    private static UnderstandingMode ResolveEffectiveMode(UnderstandingMode mode, PerceptionState perception, int frameCount)
    {
        if (mode != UnderstandingMode.Auto)
            return mode;
        if (perception.GameState != null)
            return UnderstandingMode.GameStateFirst;
        if (frameCount > 1)
            return UnderstandingMode.Temporal;
        return UnderstandingMode.PixelOnly;
    }

    private static IReadOnlyDictionary<string, string> BuildTemplateVars(
        PerceptionState perception,
        string goal,
        string[] constraints,
        int frameCount,
        string? audioTranscript,
        bool watchMode,
        UnderstandingMode effectiveMode)
    {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["goal"] = goal ?? "",
            ["frameCount"] = frameCount.ToString(),
            ["constraintsSection"] = constraints is { Length: > 0 }
                ? "## Constraints:\n" + string.Join("\n", constraints.Select(c => $"- {c}")) + "\n"
                : "",
            ["gameStateSection"] = "",
            ["audioSection"] = "",
            ["multiFrameIntro"] = "",
            ["firstTimeUserHint"] = ""
        };

        if (effectiveMode == UnderstandingMode.GameStateFirst && perception.GameState != null)
        {
            var gs = perception.GameState;
            var sb = new StringBuilder();
            sb.AppendLine("## Game State (from game engine - prioritize this over pixel inference):");
            sb.AppendLine($"- Scene: {gs.CurrentScene ?? "unknown"}");
            sb.AppendLine($"- Level: {gs.CurrentLevel ?? "unknown"}");
            sb.AppendLine($"- Paused: {gs.IsPaused}, InMenu: {gs.IsInMenu}, Loading: {gs.IsLoading}");
            if (perception.PlayerState != null)
            {
                sb.AppendLine($"- Player: Health={perception.PlayerState.Health}");
                if (perception.PlayerState.Inventory.Count > 0)
                    sb.AppendLine($"- Inventory: {string.Join(", ", perception.PlayerState.Inventory)}");
            }
            sb.AppendLine();
            sb.AppendLine("Use this structured state as the primary source of truth. Use the screenshot to identify UI elements (buttons, HUD) and pixel coordinates for actions.");
            sb.AppendLine();
            vars["gameStateSection"] = sb.ToString();
        }

        if (!string.IsNullOrWhiteSpace(audioTranscript))
        {
            vars["audioSection"] = "## Audio Transcript (from the video):\n" + audioTranscript.Trim() + "\n\nUse this transcript alongside the screenshot to understand the video content.\n\n";
        }

        vars["multiFrameIntro"] = frameCount > 1
            ? (watchMode || effectiveMode == UnderstandingMode.Temporal
                ? $"These {frameCount} images are consecutive video frames over time (oldest first). Describe the gameplay flow, changes, and notable moments.\n"
                : $"These {frameCount} images are consecutive screenshots over time (oldest first). Focus on the most recent but note any changes.\n")
            : "";

        var isFirstTimeUserGoal = !string.IsNullOrEmpty(goal) && (
            goal.Contains("first time", StringComparison.OrdinalIgnoreCase) ||
            goal.Contains("new user", StringComparison.OrdinalIgnoreCase) ||
            goal.Contains("come up with your own", StringComparison.OrdinalIgnoreCase) ||
            goal.Contains("your own questions", StringComparison.OrdinalIgnoreCase));
        vars["firstTimeUserHint"] = isFirstTimeUserGoal && !watchMode
            ? "The tester is simulating a new user. For type actions, suggest varied natural questions or messages a real first-time user might ask (e.g. 'What is this app?', 'How do I get started?', 'What can you help with?'), not generic placeholders like 'hello'.\n"
            : "";

        return vars;
    }

    private static string BuildVisionUnderstandingPrompt(
        PerceptionState perception,
        string goal,
        string[] constraints,
        int frameCount = 0,
        string? audioTranscript = null,
        bool watchMode = false,
        UnderstandingMode effectiveMode = UnderstandingMode.PixelOnly)
    {
        var sb = new StringBuilder();

        // GameState-first: lead with structured state when available (Option C)
        if (effectiveMode == UnderstandingMode.GameStateFirst && perception.GameState != null)
        {
            sb.AppendLine("## Game State (from game engine - prioritize this over pixel inference):");
            var gs = perception.GameState;
            sb.AppendLine($"- Scene: {gs.CurrentScene ?? "unknown"}");
            sb.AppendLine($"- Level: {gs.CurrentLevel ?? "unknown"}");
            sb.AppendLine($"- Paused: {gs.IsPaused}, InMenu: {gs.IsInMenu}, Loading: {gs.IsLoading}");
            if (perception.PlayerState != null)
            {
                sb.AppendLine($"- Player: Health={perception.PlayerState.Health}");
                if (perception.PlayerState.Inventory.Count > 0)
                    sb.AppendLine($"- Inventory: {string.Join(", ", perception.PlayerState.Inventory)}");
            }
            sb.AppendLine();
            sb.AppendLine("Use this structured state as the primary source of truth. Use the screenshot to identify UI elements (buttons, HUD) and pixel coordinates for actions.");
            sb.AppendLine();
        }

        if (frameCount > 1)
            sb.AppendLine(watchMode || effectiveMode == UnderstandingMode.Temporal
                ? $"These {frameCount} images are consecutive video frames over time (oldest first). Describe the gameplay flow, changes, and notable moments."
                : $"These {frameCount} images are consecutive screenshots over time (oldest first). Focus on the most recent but note any changes.");
        if (!string.IsNullOrWhiteSpace(audioTranscript))
        {
            sb.AppendLine("## Audio Transcript (from the video):");
            sb.AppendLine(audioTranscript.Trim());
            sb.AppendLine("Use this transcript alongside the screenshot to understand the video content.");
            sb.AppendLine();
        }
        sb.AppendLine(watchMode ? "## Analysis Request:" : "## Testing Goal:");
        sb.AppendLine(goal);
        if (constraints.Length > 0)
        {
            sb.AppendLine("## Constraints:");
            foreach (var c in constraints) sb.AppendLine($"- {c}");
        }
        sb.AppendLine();
        if (watchMode)
        {
            sb.AppendLine("Return JSON only (no markdown):");
            sb.AppendLine(@"{
  ""screenType"": ""e.g. Action game combat, RPG menu, platformer level"",
  ""currentContext"": ""Your live summary: what is happening, game state, player actions, notable events"",
  ""availableActions"": [],
  ""currentObjective"": ""Observation/summary"",
  ""progressPercent"": 0,
  ""issues"": [],
  ""unexploredAreas"": [],
  ""confidence"": 0.8
}");
            return sb.ToString();
        }
        var isFirstTimeUserGoal = goal.Contains("first time", StringComparison.OrdinalIgnoreCase)
            || goal.Contains("new user", StringComparison.OrdinalIgnoreCase)
            || goal.Contains("come up with your own", StringComparison.OrdinalIgnoreCase)
            || goal.Contains("your own questions", StringComparison.OrdinalIgnoreCase);
        if (isFirstTimeUserGoal)
            sb.AppendLine("The tester is simulating a new user. For type actions, suggest varied natural questions or messages a real first-time user might ask (e.g. 'What is this app?', 'How do I get started?', 'What can you help with?'), not generic placeholders like 'hello'.");
        sb.AppendLine("Look at the screenshot as a human would. Identify: buttons (e.g. Send, Chat, Architecture), text input areas, labels. For each action a user could take, give the approximate pixel coordinates of where to click (target \"x,y\", origin top-left of screen). For typing, give the coordinates of the input field and \"value\": \"text to type\".");
        sb.AppendLine("Return JSON only (no markdown):");
        sb.AppendLine(@"{
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
}");
        return sb.ToString();
    }

    private static Understanding BuildDeterministicUnderstanding(PerceptionState perception, string goal)
    {
        // Minimal deterministic summary, prioritizing known interactive elements.
        var screenType = !string.IsNullOrWhiteSpace(perception.CurrentUrl) ? "Web"
            : perception.GameState != null ? "Game"
            : "Unknown";

        var actions = perception.InteractiveElements
            .Take(40)
            .Select(el => new AvailableAction
            {
                Id = el.Id,
                Description = el.Label ?? el.Description ?? el.Id,
                Type = "click",
                Target = el.Id,
                RelevanceToGoal = 0.5,
                ExplorationValue = 0.5,
                RiskLevel = 0.1
            })
            .ToList();

        return new Understanding
        {
            ScreenType = screenType,
            CurrentContext = $"Deterministic analysis ({screenType}). Goal: {goal}",
            CurrentObjective = "Explore available actions",
            ProgressPercent = 0,
            Confidence = 0.6,
            AvailableActions = actions,
            Issues = Array.Empty<DetectedIssue>(),
            UnexploredAreas = Array.Empty<string>()
        };
    }
    
    private static string BuildUnderstandingPrompt(
        PerceptionState perception,
        string goal,
        string[] constraints,
        TestAction[] actionHistory)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"## Testing Goal: {goal}");
        sb.AppendLine();
        
        if (constraints.Length > 0)
        {
            sb.AppendLine("## Constraints:");
            foreach (var constraint in constraints)
                sb.AppendLine($"- {constraint}");
            sb.AppendLine();
        }
        
        sb.AppendLine("## Current State:");
        if (!string.IsNullOrEmpty(perception.WindowTitle))
            sb.AppendLine($"Window: {perception.WindowTitle}");
        if (!string.IsNullOrEmpty(perception.CurrentUrl))
            sb.AppendLine($"URL: {perception.CurrentUrl}");
        if (perception.GameState != null)
        {
            sb.AppendLine($"Game Scene: {perception.GameState.CurrentScene}");
            sb.AppendLine($"In Menu: {perception.GameState.IsInMenu}");
        }
        
        if (perception.InteractiveElements.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Interactive Elements (top 20):");
            foreach (var el in perception.InteractiveElements.Take(20))
            {
                var desc = el.Label ?? el.Description ?? el.Id;
                sb.AppendLine($"- [{el.Type}] {desc}");
            }
        }
        
        if (perception.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Errors:");
            foreach (var error in perception.Errors.Take(10))
                sb.AppendLine($"- {error}");
        }
        
        sb.AppendLine();
        var isFirstTimeUser = goal.Contains("first time", StringComparison.OrdinalIgnoreCase)
            || goal.Contains("new user", StringComparison.OrdinalIgnoreCase)
            || goal.Contains("your own questions", StringComparison.OrdinalIgnoreCase);
        if (isFirstTimeUser)
            sb.AppendLine("The tester is simulating a new user. Suggest varied natural questions or messages for type actions (e.g. 'What is this?', 'How do I start?'), not only generic text.");
        sb.AppendLine(@"## Your Task

Analyze and provide JSON:
{
  ""screenType"": ""..."",
  ""currentContext"": ""..."",
  ""availableActions"": [
    { ""id"": ""..."", ""description"": ""..."", ""type"": ""click"", ""relevanceToGoal"": 0.8, ""explorationValue"": 0.5, ""riskLevel"": 0.1 }
  ],
  ""currentObjective"": ""..."",
  ""progressPercent"": 50,
  ""issues"": [],
  ""unexploredAreas"": [],
  ""confidence"": 0.8
}");
        
        return sb.ToString();
    }
    
    private static Understanding ParseUnderstanding(string json, PerceptionState perception)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            return new Understanding
            {
                ScreenType = root.TryGetProperty("screenType", out var st) ? st.GetString() ?? "Unknown" : "Unknown",
                CurrentContext = root.TryGetProperty("currentContext", out var cc) ? cc.GetString() ?? "" : "",
                CurrentObjective = root.TryGetProperty("currentObjective", out var co) ? co.GetString() ?? "" : "",
                ProgressPercent = root.TryGetProperty("progressPercent", out var pp) ? pp.GetInt32() : 0,
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5,
                
                AvailableActions = root.TryGetProperty("availableActions", out var actions)
                    ? actions.EnumerateArray().Select(ParseAction).ToList()
                    : Array.Empty<AvailableAction>(),
                
                Issues = root.TryGetProperty("issues", out var issues)
                    ? issues.EnumerateArray().Select(ParseIssue).ToList()
                    : Array.Empty<DetectedIssue>(),
                
                UnexploredAreas = root.TryGetProperty("unexploredAreas", out var areas)
                    ? areas.EnumerateArray().Select(a => a.GetString()!).ToList()
                    : Array.Empty<string>()
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Understanding brick could not parse vision/LLM response as JSON. Ensure the model returns valid JSON.", ex);
        }
    }
    
    private static AvailableAction ParseAction(JsonElement el) => new()
    {
        Id = el.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
        Description = el.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
        Type = el.TryGetProperty("type", out var type) ? type.GetString() ?? "click" : "click",
        Target = el.TryGetProperty("target", out var t) ? t.GetString() : null,
        Value = el.TryGetProperty("value", out var v) ? v.GetString() : null,
        RelevanceToGoal = el.TryGetProperty("relevanceToGoal", out var r) ? r.GetDouble() : 0.5,
        ExplorationValue = el.TryGetProperty("explorationValue", out var e) ? e.GetDouble() : 0.5,
        RiskLevel = el.TryGetProperty("riskLevel", out var risk) ? risk.GetDouble() : 0.1
    };
    
    private static DetectedIssue ParseIssue(JsonElement el) => new()
    {
        Type = el.TryGetProperty("type", out var t) ? t.GetString() ?? "unknown" : "unknown",
        Description = el.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
        Severity = Enum.TryParse<IssueSeverity>(
            el.TryGetProperty("severity", out var s) ? s.GetString() : "Low", 
            true, out var sev) ? sev : IssueSeverity.Low
    };
}
