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
/// AI decides what action to take next.
/// </summary>
public class ExplorationBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<ExplorationBrick>? _logger;
    
    public ExplorationBrick(IProviderFactory providerFactory, ILogger<ExplorationBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "universal-tester.exploration";
        Name = "Exploration";
        Version = "1.0.0";
        Icon = "🔍";
        Category = BrickCategory.Control;
        Description = "AI decides what action to take next";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("understanding", "Understanding", "Current understanding"),
                new BrickInputDefinition("goal", "string", "Testing goal"),
                new BrickInputDefinition("depth", "TestingDepth", "Testing depth"),
                new BrickInputDefinition("actionHistory", "TestAction[]", "Previous actions", required: false)
            ],
            Outputs = [
                new BrickOutputDefinition("nextAction", "TestAction", "Next action to execute"),
                new BrickOutputDefinition("reasoning", "string", "Why this action was chosen"),
                new BrickOutputDefinition("shouldStop", "bool", "Whether to stop testing")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "exploration-deterministic",
                Name = "Deterministic Exploration",
                Description = "Selects highest scoring action",
                Executor = "DirectExecutor",
                Config = new Dictionary<string, object>()
            },
            Agentic = new AgenticImplementation
            {
                Id = "exploration-agentic",
                Name = "AI Exploration",
                Description = "AI decides next action",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are deciding what action to take next in testing.",
                    Temperature = 0.5,
                    MaxTokens = 1000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "2-5s",
                    Deterministic = false,
                    RequiresNetwork = true,
                    ResourceUsage = ResourceUsage.Medium
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
        var understanding = input.Get<Understanding>("understanding");
        var goal = input.Get<string>("goal");
        var depth = input.Get<TestingDepth>("depth");
        var actionHistory = input.Get<TestAction[]>("actionHistory", Array.Empty<TestAction>()) ?? Array.Empty<TestAction>();
        
        if (implementation == ImplementationType.Deterministic)
        {
            return ExecuteDeterministic(understanding, depth, actionHistory);
        }
        
        // Agentic mode
        var prompt = BuildExplorationPrompt(understanding, goal, depth, actionHistory);
        
        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are deciding what action to take next in testing.",
            prompt,
            new { },
            cancellationToken);
        
        var (nextAction, reasoning, shouldStop) = ParseExplorationOutput(response, understanding);
        
        return new BrickOutput
        {
            ["nextAction"] = nextAction,
            ["reasoning"] = reasoning,
            ["shouldStop"] = shouldStop,
            Summary = $"Selected: {nextAction.Description}"
        };
    }
    
    private static BrickOutput ExecuteDeterministic(Understanding understanding, TestingDepth depth, TestAction[] actionHistory)
    {
        var actions = understanding.AvailableActions;
        var maxSteps = depth switch
        {
            TestingDepth.Quick => 10,
            TestingDepth.Standard => 30,
            TestingDepth.Thorough => 80,
            TestingDepth.Exhaustive => 150,
            _ => 30
        };
        
        if (!actions.Any())
        {
            var waitAction = new TestAction
            {
                Id = "wait",
                Type = ActionType.Wait,
                Description = "No actions available, waiting",
                Duration = TimeSpan.FromSeconds(1)
            };
            
            return new BrickOutput
            {
                ["nextAction"] = waitAction,
                ["reasoning"] = "No actions available",
                ["shouldStop"] = true,
                Summary = "Waiting - no actions"
            };
        }
        
        // Score actions
        var scoredActions = actions
            .Select(a => new
            {
                Action = a,
                Score = (a.RelevanceToGoal * 0.6) + (a.ExplorationValue * 0.3) - (a.RiskLevel * 0.1)
            })
            .OrderByDescending(x => x.Score)
            .ToList();
        
        var best = scoredActions.First();
        var testAction = ConvertToTestAction(best.Action);
        
        return new BrickOutput
        {
            ["nextAction"] = testAction,
            ["reasoning"] = actionHistory.Length >= maxSteps
                ? $"Reached deterministic step cap ({maxSteps})"
                : $"Selected highest scoring action (score: {best.Score:F2})",
            ["shouldStop"] = understanding.ProgressPercent >= 100 || actionHistory.Length >= maxSteps,
            Summary = $"Selected: {testAction.Description}"
        };
    }
    
    private static string BuildExplorationPrompt(
        Understanding understanding,
        string goal,
        TestingDepth depth,
        TestAction[] actionHistory)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"## Goal: {goal}");
        sb.AppendLine($"## Progress: {understanding.ProgressPercent}%");
        sb.AppendLine($"## Current Context: {understanding.CurrentContext}");
        sb.AppendLine($"## Testing Depth: {depth}");
        sb.AppendLine();
        
        sb.AppendLine("## Available Actions:");
        foreach (var action in understanding.AvailableActions)
        {
            sb.AppendLine($"- {action.Id}: {action.Description}");
            sb.AppendLine($"  Relevance: {action.RelevanceToGoal:F1}, Exploration: {action.ExplorationValue:F1}, Risk: {action.RiskLevel:F1}");
        }
        
        sb.AppendLine();
        sb.AppendLine(@"## Decision

Provide JSON:
{
  ""nextActionId"": ""action_id"",
  ""reasoning"": ""why this action"",
  ""shouldStop"": false
}");
        
        return sb.ToString();
    }
    
    private static (TestAction, string, bool) ParseExplorationOutput(string json, Understanding understanding)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var nextActionId = root.TryGetProperty("nextActionId", out var id) ? id.GetString() : null;
            var action = understanding.AvailableActions.FirstOrDefault(a => a.Id == nextActionId)
                ?? understanding.AvailableActions.FirstOrDefault();
            
            var reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() ?? "" : "";
            var shouldStop = root.TryGetProperty("shouldStop", out var stop) && stop.GetBoolean();
            
            return (action != null ? ConvertToTestAction(action) : new TestAction
            {
                Id = "wait",
                Type = ActionType.Wait,
                Description = "Waiting",
                Duration = TimeSpan.FromSeconds(1)
            }, reasoning, shouldStop);
        }
        catch
        {
            // Fallback
            var action = understanding.AvailableActions.FirstOrDefault();
            return (action != null ? ConvertToTestAction(action) : new TestAction
            {
                Id = "wait",
                Type = ActionType.Wait,
                Description = "Waiting",
                Duration = TimeSpan.FromSeconds(1)
            }, "Failed to parse AI response", false);
        }
    }
    
    private static TestAction ConvertToTestAction(AvailableAction action)
    {
        var actionType = action.Type.ToLowerInvariant() switch
        {
            "click" => ActionType.Click,
            "type" => ActionType.Type,
            "navigate" => ActionType.Navigate,
            "scroll" => ActionType.Scroll,
            "hover" => ActionType.Hover,
            "move" => ActionType.Move,
            "attack" => ActionType.Attack,
            "jump" => ActionType.Jump,
            "interact" => ActionType.Interact,
            "keypress" or "key_press" => ActionType.KeyPress,
            "api_call" or "apirequest" => ActionType.ApiRequest,
            "command" => ActionType.ExecuteCommand,
            _ => ActionType.Click
        };
        
        return new TestAction
        {
            Id = action.Id,
            Type = actionType,
            Description = action.Description,
            Selector = action.Target,
            InputValue = action.Value
        };
    }
}
