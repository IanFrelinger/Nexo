using Microsoft.Extensions.Logging;
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
    private readonly ILogger<UnderstandingBrick>? _logger;
    
    public UnderstandingBrick(IProviderFactory providerFactory, ILogger<UnderstandingBrick>? logger = null)
    {
        _providerFactory = providerFactory;
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
                new BrickInputDefinition("previousUnderstanding", "Understanding", "Previous understanding", required: false)
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

        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are a universal testing agent analyzing an application.",
            prompt,
            new { },
            cancellationToken);

        var understandingAgentic = ParseUnderstanding(response, perception);
        
        return new BrickOutput
        {
            ["understanding"] = understandingAgentic,
            Summary = $"Understood: {understandingAgentic.ScreenType}, {understandingAgentic.AvailableActions.Count} actions available"
        };
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
        catch
        {
            // Fallback understanding
            return new Understanding
            {
                ScreenType = "Unknown",
                CurrentContext = "Unable to parse AI response",
                CurrentObjective = "Continue exploration",
                ProgressPercent = 0,
                Confidence = 0.1,
                AvailableActions = perception.InteractiveElements.Select((el, i) => new AvailableAction
                {
                    Id = el.Id,
                    Description = el.Label ?? el.Type,
                    Type = "click",
                    Target = el.Id,
                    RelevanceToGoal = 0.5,
                    ExplorationValue = 0.5,
                    RiskLevel = 0.1
                }).ToList()
            };
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
