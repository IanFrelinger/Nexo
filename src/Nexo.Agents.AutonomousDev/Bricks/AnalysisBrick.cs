using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.AutonomousDev.Bricks;

/// <summary>
/// Analyzes test feedback and decides next steps.
/// </summary>
public class AnalysisBrick : Brick
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<AnalysisBrick>? _logger;
    
    public AnalysisBrick(IProviderFactory providerFactory, ILogger<AnalysisBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "autonomous-dev.analysis";
        Name = "Analysis";
        Version = "1.0.0";
        Icon = "🔍";
        Category = BrickCategory.Analysis;
        Description = "Analyzes test feedback and decides next steps";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("feedback", "TestFeedback", "Test feedback"),
                new BrickInputDefinition("specification", "Specification", "Feature specification"),
                new BrickInputDefinition("plan", "DevelopmentPlan", "Development plan"),
                new BrickInputDefinition("currentArtifacts", "GeneratedArtifact[]", "Current artifacts"),
                new BrickInputDefinition("currentIteration", "int", "Current iteration"),
                new BrickInputDefinition("maxIterations", "int", "Maximum iterations")
            ],
            Outputs = [
                new BrickOutputDefinition("decision", "IterationDecision", "Decision on next steps")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "analysis-deterministic",
                Name = "Heuristic Analysis",
                Description = "Decides next steps using simple rules",
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
                Id = "analysis-agentic",
                Name = "AI Analysis",
                Description = "Uses LLM to analyze and decide",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are analyzing test feedback to decide what to do next.",
                    Temperature = 0.3,
                    MaxTokens = 2000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "3-10s",
                    Deterministic = false,
                    RequiresNetwork = true,
                    ResourceUsage = ResourceUsage.Medium
                }
            }
        };
        
        DefaultImplementation = ImplementationType.Agentic;
        FallbackChain = new[] { ImplementationType.Agentic, ImplementationType.Deterministic };
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var feedback = input.Get<TestFeedback>("feedback");
        var specification = input.Get<Specification>("specification");
        var plan = input.Get<DevelopmentPlan>("plan");
        var currentArtifacts = input.Get<GeneratedArtifact[]>("currentArtifacts");
        var currentIteration = input.Get<int>("currentIteration");
        var maxIterations = input.Get<int>("maxIterations");
        
        // Check obvious conditions first
        if (feedback.OverallSuccess && feedback.AcceptanceScore >= 90)
        {
            return new BrickOutput
            {
                ["decision"] = new IterationDecision
                {
                    Decision = DecisionType.Complete,
                    Reasoning = $"All acceptance criteria met. Score: {feedback.AcceptanceScore}%",
                    Confidence = 0.95
                },
                Summary = "Complete - all criteria met"
            };
        }
        
        if (currentIteration >= maxIterations)
        {
            return new BrickOutput
            {
                ["decision"] = new IterationDecision
                {
                    Decision = DecisionType.MaxIterationsReached,
                    Reasoning = $"Reached maximum iterations ({maxIterations})",
                    Confidence = 1.0,
                    AbortReason = "Maximum iterations reached"
                },
                Summary = "Max iterations reached"
            };
        }

        // Deterministic fallback: if we have actionable items, iterate once; else abort.
        if (implementation == ImplementationType.Deterministic || context.IsAirGapped)
        {
            var deterministicDecision = new IterationDecision
            {
                Decision = feedback.ActionableItems.Count > 0 ? DecisionType.Iterate : DecisionType.Stuck,
                Reasoning = feedback.ActionableItems.Count > 0
                    ? "Deterministic: actionable items exist; iterate once."
                    : "Deterministic: no actionable items; cannot proceed.",
                PlannedFixes = feedback.ActionableItems
                    .Take(3)
                    .Select((i, idx) => new PlannedFix
                    {
                        Description = i.SuggestedAction,
                        ApproachDescription = i.Problem,
                        IssueId = $"issue{idx + 1}",
                        FilesToModify = i.AffectedFiles ?? Array.Empty<string>(),
                        ConfidenceInFix = 0.7
                    })
                    .ToList(),
                Confidence = 0.75,
                EstimatedRemainingIterations = 1
            };

            return new BrickOutput
            {
                ["decision"] = deterministicDecision,
                Summary = $"Decision (deterministic): {deterministicDecision.Decision} - {deterministicDecision.Reasoning}"
            };
        }
        
        // Use AI to analyze
        var prompt = BuildAnalysisPrompt(feedback, specification, currentArtifacts, currentIteration, maxIterations);
        
        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are analyzing test feedback to decide what to do next.",
            prompt,
            new { },
            cancellationToken);
        
        var decision = ParseIterationDecision(response);
        
        return new BrickOutput
        {
            ["decision"] = decision,
            Summary = $"Decision: {decision.Decision} - {decision.Reasoning}"
        };
    }
    
    private static string BuildAnalysisPrompt(
        TestFeedback feedback,
        Specification spec,
        GeneratedArtifact[] artifacts,
        int iteration,
        int maxIterations)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"## Iteration {iteration} of {maxIterations}");
        sb.AppendLine();
        sb.AppendLine($"Success: {feedback.OverallSuccess}");
        sb.AppendLine($"Score: {feedback.AcceptanceScore}%");
        sb.AppendLine();
        sb.AppendLine("User Experience:");
        sb.AppendLine(feedback.UserExperience.Narrative);
        sb.AppendLine();
        sb.AppendLine("Issues:");
        foreach (var issue in feedback.Issues)
            sb.AppendLine($"- [{issue.Severity}] {issue.Type}: {issue.Description}");
        sb.AppendLine();
        sb.AppendLine("Actionable Feedback:");
        foreach (var item in feedback.ActionableItems)
            sb.AppendLine($"- [{item.Priority}] {item.Problem}: {item.SuggestedAction}");
        
        sb.AppendLine();
        sb.AppendLine(@"## Your Task

Decide next steps. Respond in JSON:

{
  ""decision"": ""Complete|Iterate|NeedsClarification|Stuck|MaxIterationsReached|NeedsRedesign"",
  ""reasoning"": ""..."",
  ""plannedFixes"": [
    {
      ""issueId"": ""..."",
      ""description"": ""..."",
      ""approachDescription"": ""..."",
      ""filesToModify"": [""..."", ""...""],
      ""confidenceInFix"": 0.8
    }
  ],
  ""confidence"": 0.8,
  ""estimatedRemainingIterations"": 2
}");
        
        return sb.ToString();
    }
    
    private static IterationDecision ParseIterationDecision(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            return new IterationDecision
            {
                Decision = Enum.TryParse<DecisionType>(
                    root.GetProperty("decision").GetString() ?? "Iterate",
                    true, out var decision) ? decision : DecisionType.Iterate,
                Reasoning = root.GetProperty("reasoning").GetString() ?? "",
                Confidence = root.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.7,
                EstimatedRemainingIterations = root.TryGetProperty("estimatedRemainingIterations", out var est) ? est.GetInt32() : 3,
                PlannedFixes = ParsePlannedFixes(root),
                AbortReason = root.TryGetProperty("abortReason", out var abort) ? abort.GetString() : null
            };
        }
        catch
        {
            return new IterationDecision
            {
                Decision = DecisionType.Iterate,
                Reasoning = "Failed to parse decision, continuing iteration",
                Confidence = 0.5
            };
        }
    }
    
    private static IReadOnlyList<PlannedFix> ParsePlannedFixes(JsonElement root)
    {
        if (!root.TryGetProperty("plannedFixes", out var fixes))
            return Array.Empty<PlannedFix>();
        
        return fixes.EnumerateArray().Select(f => new PlannedFix
        {
            IssueId = f.GetProperty("issueId").GetString() ?? "",
            Description = f.GetProperty("description").GetString() ?? "",
            ApproachDescription = f.GetProperty("approachDescription").GetString() ?? "",
            FilesToModify = f.TryGetProperty("filesToModify", out var files)
                ? files.EnumerateArray().Select(x => x.GetString()!).ToList()
                : Array.Empty<string>(),
            ConfidenceInFix = f.TryGetProperty("confidenceInFix", out var conf) ? conf.GetDouble() : 0.7
        }).ToList();
    }
}
