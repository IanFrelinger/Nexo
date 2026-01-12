using Microsoft.Extensions.Logging;
using Nexo.Agents.AutonomousDev.Models;
using Nexo.Agents.UniversalTester;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.AutonomousDev.Bricks;

/// <summary>
/// Runs Universal Tester as a mock user to provide feedback.
/// </summary>
public class TestingBrick : Brick
{
    private readonly UniversalTesterAgent _tester;
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<TestingBrick>? _logger;
    
    public TestingBrick(
        UniversalTesterAgent tester,
        IProviderFactory providerFactory,
        ILogger<TestingBrick>? logger = null)
    {
        _tester = tester;
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "autonomous-dev.testing";
        Name = "Testing";
        Version = "1.0.0";
        Icon = "🧪";
        Category = BrickCategory.Validation;
        Description = "Runs Universal Tester as a mock user to provide feedback";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("specification", "Specification", "Feature specification"),
                new BrickInputDefinition("strategy", "TestingStrategy", "Testing strategy"),
                new BrickInputDefinition("testTarget", "string", "Test target"),
                new BrickInputDefinition("iterationNumber", "int", "Iteration number")
            ],
            Outputs = [
                new BrickOutputDefinition("feedback", "TestFeedback", "Test feedback")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Agentic = new AgenticImplementation
            {
                Id = "testing-agentic",
                Name = "AI Testing",
                Description = "Uses Universal Tester with AI analysis",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are analyzing test results to provide developer feedback.",
                    Temperature = 0.2,
                    MaxTokens = 3000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "30-120s",
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
        var specification = input.Get<Specification>("specification");
        var strategy = input.Get<TestingStrategy>("strategy");
        var testTarget = input.Get<string>("testTarget");
        var iterationNumber = input.Get<int>("iterationNumber");
        
        var testGoal = BuildTestGoal(specification, strategy);
        
        var testerConfig = new UniversalTesterConfig
        {
            Target = testTarget,
            Goal = testGoal,
            Depth = TestingDepth.Standard,
            MaxDuration = TimeSpan.FromMinutes(5),
            Constraints = GetPersonaConstraints(strategy.Persona)
        };
        
        // Run Universal Tester
        var testReport = await _tester.ExecuteAsync(testerConfig, context, cancellationToken);
        
        // Analyze results to create feedback
        var feedback = await AnalyzeTestResultsAsync(
            specification,
            strategy,
            testReport,
            iterationNumber,
            context,
            cancellationToken);
        
        return new BrickOutput
        {
            ["feedback"] = feedback,
            Summary = $"Test feedback: Score {feedback.AcceptanceScore}%, {feedback.Issues.Count} issues"
        };
    }
    
    private static string BuildTestGoal(Specification spec, TestingStrategy strategy)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Test the following feature: {spec.Summary}");
        sb.AppendLine();
        sb.AppendLine("Acceptance Criteria to verify:");
        
        foreach (var criterion in spec.AcceptanceCriteria)
        {
            sb.AppendLine($"- {criterion.Description}");
            sb.AppendLine($"  Test: {criterion.TestDescription}");
        }
        
        if (strategy.UserFlows.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("User flows to test:");
            foreach (var flow in strategy.UserFlows)
                sb.AppendLine($"- {flow}");
        }
        
        return sb.ToString();
    }
    
    private static string[] GetPersonaConstraints(MockUserPersona persona)
    {
        return persona switch
        {
            MockUserPersona.Novice => new[]
            {
                "Act like a first-time user",
                "Don't assume knowledge of shortcuts",
                "Report confusion points"
            },
            MockUserPersona.PowerUser => new[]
            {
                "Try keyboard shortcuts",
                "Attempt edge cases",
                "Look for efficiency"
            },
            MockUserPersona.Adversarial => new[]
            {
                "Try to break the application",
                "Enter invalid inputs",
                "Click rapidly"
            },
            _ => new[] { "Act like a typical user" }
        };
    }
    
    private async Task<TestFeedback> AnalyzeTestResultsAsync(
        Specification spec,
        TestingStrategy strategy,
        TestReport testReport,
        int iteration,
        IExecutionContext context,
        CancellationToken ct)
    {
        var prompt = $@"Analyze test results as a {strategy.Persona} user.

## Feature: {spec.Summary}

## Acceptance Criteria
{string.Join("\n", spec.AcceptanceCriteria.Select(c => $"- {c.Id}: {c.Description}"))}

## Test Results
Score: {testReport.Summary.OverallScore}%
Passed: {testReport.Summary.Passed}/{testReport.Summary.TotalTests}
Findings: {string.Join(", ", testReport.Summary.KeyFindings)}

Provide JSON feedback matching TestFeedback model.";

        var response = await _providerFactory.ExecuteLLMAsync(
            context.Provider,
            "You are analyzing test results to provide developer feedback.",
            prompt,
            new { },
            ct);
        
        return ParseTestFeedback(response, iteration, testReport);
    }
    
    private static TestFeedback ParseTestFeedback(string json, int iteration, TestReport rawReport)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            return new TestFeedback
            {
                IterationNumber = iteration,
                OverallSuccess = root.TryGetProperty("overallSuccess", out var os) && os.GetBoolean(),
                AcceptanceScore = root.TryGetProperty("acceptanceScore", out var score) ? score.GetDouble() : 0,
                CriteriaResults = ParseCriteriaResults(root),
                UserExperience = ParseUserExperience(root),
                Issues = ParseIssues(root),
                ActionableItems = ParseActionableItems(root),
                RawTestReport = rawReport
            };
        }
        catch
        {
            // Fallback feedback
            return new TestFeedback
            {
                IterationNumber = iteration,
                OverallSuccess = false,
                AcceptanceScore = 0,
                UserExperience = new MockUserExperience
                {
                    Narrative = "Failed to parse test results",
                    OverallReaction = "Error"
                },
                RawTestReport = rawReport
            };
        }
    }
    
    private static IReadOnlyList<CriterionResult> ParseCriteriaResults(JsonElement root)
    {
        if (!root.TryGetProperty("criteriaResults", out var results))
            return Array.Empty<CriterionResult>();
        
        return results.EnumerateArray().Select(r => new CriterionResult
        {
            CriterionId = r.GetProperty("criterionId").GetString() ?? "",
            Description = r.GetProperty("description").GetString() ?? "",
            Passed = r.TryGetProperty("passed", out var p) && p.GetBoolean(),
            Observation = r.GetProperty("observation").GetString() ?? ""
        }).ToList();
    }
    
    private static MockUserExperience ParseUserExperience(JsonElement root)
    {
        if (!root.TryGetProperty("userExperience", out var ux))
        {
            return new MockUserExperience
            {
                Narrative = "No user experience data",
                OverallReaction = "Unknown"
            };
        }
        
        return new MockUserExperience
        {
            Narrative = ux.GetProperty("narrative").GetString() ?? "",
            IntuitivenessScore = ux.TryGetProperty("intuitivenessScore", out var score) ? score.GetInt32() : 5,
            UserFrustrated = ux.TryGetProperty("userFrustrated", out var fr) && fr.GetBoolean(),
            TimeToComplete = ux.TryGetProperty("timeToComplete", out var time) && TimeSpan.TryParse(time.GetString(), out var ts) ? ts : TimeSpan.Zero,
            AttemptCount = ux.TryGetProperty("attemptCount", out var att) ? att.GetInt32() : 1,
            OverallReaction = ux.GetProperty("overallReaction").GetString() ?? ""
        };
    }
    
    private static IReadOnlyList<DiscoveredIssue> ParseIssues(JsonElement root)
    {
        if (!root.TryGetProperty("issues", out var issues))
            return Array.Empty<DiscoveredIssue>();
        
        return issues.EnumerateArray().Select(i => new DiscoveredIssue
        {
            Id = i.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
            Type = Enum.TryParse<IssueType>(i.TryGetProperty("type", out var t) ? t.GetString() : "Bug", true, out var type) ? type : IssueType.Bug,
            Severity = Enum.TryParse<Models.IssueSeverity>(i.TryGetProperty("severity", out var s) ? s.GetString() : "Medium", true, out var sev) ? sev : Models.IssueSeverity.Medium,
            Description = i.GetProperty("description").GetString() ?? "",
            SuggestedFix = i.GetProperty("suggestedFix").GetString() ?? ""
        }).ToList();
    }
    
    private static IReadOnlyList<ActionableFeedback> ParseActionableItems(JsonElement root)
    {
        if (!root.TryGetProperty("actionableItems", out var items))
            return Array.Empty<ActionableFeedback>();
        
        return items.EnumerateArray().Select(a => new ActionableFeedback
        {
            Id = a.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
            Priority = Enum.TryParse<FeedbackPriority>(a.TryGetProperty("priority", out var p) ? p.GetString() : "Medium", true, out var pri) ? pri : FeedbackPriority.Medium,
            Problem = a.GetProperty("problem").GetString() ?? "",
            SuggestedAction = a.GetProperty("suggestedAction").GetString() ?? "",
            AffectedFiles = a.TryGetProperty("affectedFiles", out var files) ? files.EnumerateArray().Select(f => f.GetString()!).ToList() : Array.Empty<string>()
        }).ToList();
    }
}
