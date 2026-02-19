using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// AI validates if the action produced expected results.
/// </summary>
public class ValidationBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<ValidationBrick>? _logger;
    
    public ValidationBrick(IProviderFactory providerFactory, ILogger<ValidationBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "universal-tester.validation";
        Name = "Validation";
        Version = "1.0.0";
        Icon = "✅";
        Category = BrickCategory.Validation;
        Description = "AI validates if the action produced expected results";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("action", "TestAction", "Action that was executed"),
                new BrickInputDefinition("executionResult", "ActionExecutionResult", "Execution result"),
                new BrickInputDefinition("goal", "string", "Testing goal"),
                new BrickInputDefinition("expectedOutcome", "string", "Expected outcome", required: false),
                new BrickInputDefinition("modelOverride", "string", "Per-brick model override", required: false)
            ],
            Outputs = [
                new BrickOutputDefinition("validation", "ValidationResult", "Validation result")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "validation-deterministic",
                Name = "Deterministic Validation",
                Description = "Checks for errors only",
                Executor = "DirectExecutor",
                Config = new Dictionary<string, object>()
            },
            Agentic = new AgenticImplementation
            {
                Id = "validation-agentic",
                Name = "AI Validation",
                Description = "AI analyzes outcome",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are validating the result of a test action.",
                    Temperature = 0.2,
                    MaxTokens = 1500
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
        var action = input.Get<TestAction>("action");
        var executionResult = input.Get<ActionExecutionResult>("executionResult");
        var goal = input.Get<string>("goal");
        var expectedOutcome = input.Get<string?>("expectedOutcome", null);
        var modelOverride = input.Get<string>("modelOverride", null);

        if (implementation == ImplementationType.Deterministic)
        {
            return ExecuteDeterministic(executionResult);
        }
        
        // Agentic validation
        var prompt = BuildValidationPrompt(action, executionResult, goal, expectedOutcome);
        
        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are validating the result of a test action.",
            prompt,
            modelOverride != null ? new { model = modelOverride } : new { },
            cancellationToken);
        
        var validation = ParseValidationOutput(response);
        
        return new BrickOutput
        {
            ["validation"] = validation,
            Summary = validation.Passed ? "Validation passed" : $"Validation failed: {validation.Reasoning}"
        };
    }
    
    private static BrickOutput ExecuteDeterministic(ActionExecutionResult executionResult)
    {
        var issues = new List<DetectedIssue>();
        
        if (!executionResult.Success)
        {
            issues.Add(new DetectedIssue
            {
                Type = "error",
                Description = executionResult.Error ?? "Action failed",
                Severity = IssueSeverity.High
            });
        }
        
        var validation = new ValidationResult
        {
            Passed = !issues.Any(),
            Reasoning = issues.Any() ? "Errors detected" : "No errors detected",
            IssuesFound = issues,
            Confidence = 0.7
        };
        
        return new BrickOutput
        {
            ["validation"] = validation,
            Summary = validation.Passed ? "No errors" : "Errors detected"
        };
    }
    
    private static string BuildValidationPrompt(
        TestAction action,
        ActionExecutionResult executionResult,
        string goal,
        string? expectedOutcome)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"## Action Performed");
        sb.AppendLine($"- Type: {action.Type}");
        sb.AppendLine($"- Description: {action.Description}");
        sb.AppendLine($"- Execution Success: {executionResult.Success}");
        
        if (!string.IsNullOrEmpty(executionResult.Error))
            sb.AppendLine($"- Error: {executionResult.Error}");
        
        sb.AppendLine();
        sb.AppendLine($"## Testing Goal: {goal}");
        
        if (!string.IsNullOrEmpty(expectedOutcome))
            sb.AppendLine($"## Expected Outcome: {expectedOutcome}");
        
        sb.AppendLine();
        sb.AppendLine(@"## Your Task

Provide JSON:
{
  ""passed"": true/false,
  ""reasoning"": ""explanation"",
  ""issues"": [
    { ""type"": ""visual_glitch|error|performance|ux"", ""description"": ""..."", ""severity"": ""low|medium|high|critical"" }
  ],
  ""confidence"": 0.0-1.0
}");
        
        return sb.ToString();
    }
    
    private static ValidationResult ParseValidationOutput(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            return new ValidationResult
            {
                Passed = root.TryGetProperty("passed", out var p) && p.GetBoolean(),
                Reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() ?? "" : "",
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.5,
                IssuesFound = root.TryGetProperty("issues", out var issues)
                    ? issues.EnumerateArray().Select(i => new DetectedIssue
                    {
                        Type = i.TryGetProperty("type", out var t) ? t.GetString() ?? "unknown" : "unknown",
                        Description = i.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                        Severity = Enum.TryParse<IssueSeverity>(
                            i.TryGetProperty("severity", out var s) ? s.GetString() : "Low",
                            true, out var sev) ? sev : IssueSeverity.Low
                    }).ToList()
                    : Array.Empty<DetectedIssue>()
            };
        }
        catch
        {
            return new ValidationResult
            {
                Passed = false,
                Reasoning = "Failed to parse validation response",
                Confidence = 0.1,
                IssuesFound = Array.Empty<DetectedIssue>()
            };
        }
    }
}
