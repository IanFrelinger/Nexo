using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Generates comprehensive test reports.
/// </summary>
public class ReportingBrick : Brick
{
    private readonly IProviderFactory? _providerFactory;
    private readonly ILogger<ReportingBrick>? _logger;
    
    public ReportingBrick(IProviderFactory? providerFactory = null, ILogger<ReportingBrick>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
        
        Id = "universal-tester.reporting";
        Name = "Reporting";
        Version = "1.0.0";
        Icon = "📊";
        Category = BrickCategory.Output;
        Description = "Generates comprehensive test reports";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("session", "TestSession", "Test session data"),
                new BrickInputDefinition("format", "string", "Report format (html/json)", required: false, defaultValue: "html")
            ],
            Outputs = [
                new BrickOutputDefinition("report", "TestReport", "Generated test report")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "reporting-deterministic",
                Name = "Deterministic Reporting",
                Description = "Generates basic report",
                Executor = "DirectExecutor",
                Config = new Dictionary<string, object>()
            },
            Agentic = new AgenticImplementation
            {
                Id = "reporting-agentic",
                Name = "AI Reporting",
                Description = "AI-enhanced report generation",
                LLMConfig = new LLMConfig
                {
                    Model = "gpt-4",
                    SystemPrompt = "You are generating a test report summary.",
                    Temperature = 0.3,
                    MaxTokens = 2000
                },
                Characteristics = new ImplementationCharacteristics
                {
                    Latency = "3-8s",
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
        var session = input.Get<TestSession>("session");
        var format = input.Get<string>("format", "html");
        
        var totalSteps = session.Steps.Count;
        var passedSteps = session.Steps.Count(s => s.Validation.Passed);
        var failedSteps = totalSteps - passedSteps;
        
        var criticalIssues = session.AllIssues.Count(i => i.Severity == IssueSeverity.Critical);
        var highIssues = session.AllIssues.Count(i => i.Severity == IssueSeverity.High);
        
        var score = CalculateScore(session);
        
        var keyFindings = new List<string>();
        var recommendations = new List<string>();
        
        // Deterministic findings
        if (criticalIssues > 0)
            keyFindings.Add($"{criticalIssues} critical issue(s) found");
        if (highIssues > 0)
            keyFindings.Add($"{highIssues} high severity issue(s) found");
        if (failedSteps > 0)
            keyFindings.Add($"{failedSteps} of {totalSteps} actions had issues");
        if (session.GoalAchieved)
            keyFindings.Add("Testing goal was achieved");
        else
            keyFindings.Add($"Testing stopped at {session.ProgressPercent}% progress: {session.StopReason}");
        
        // Agentic: AI-generated summary
        if (implementation == ImplementationType.Agentic && _providerFactory != null)
        {
            var aiSummary = await GenerateAiSummaryAsync(session, context, cancellationToken);
            keyFindings.AddRange(aiSummary.Findings);
            recommendations.AddRange(aiSummary.Recommendations);
        }
        
        var summary = new TestSummary
        {
            TotalTests = totalSteps,
            Passed = passedSteps,
            Failed = failedSteps,
            Warnings = session.AllIssues.Count(i => i.Severity == IssueSeverity.Medium),
            Duration = session.EndTime - session.StartTime,
            OverallScore = score,
            KeyFindings = keyFindings,
            Recommendations = recommendations
        };
        
        // Generate report
        var htmlReport = format == "html" ? GenerateHtmlReport(session, summary) : null;
        var jsonReport = format == "json"
            ? JsonSerializer.Serialize(new { session, summary }, new JsonSerializerOptions { WriteIndented = true })
            : null;
        
        var report = new TestReport
        {
            Summary = summary,
            HtmlReport = htmlReport,
            JsonReport = jsonReport
        };
        
        return new BrickOutput
        {
            ["report"] = report,
            Summary = $"Report generated: Score {score:F0}, {totalSteps} steps, {session.AllIssues.Count} issues"
        };
    }
    
    private static double CalculateScore(TestSession session)
    {
        var baseScore = 100.0;
        
        foreach (var issue in session.AllIssues)
        {
            baseScore -= issue.Severity switch
            {
                IssueSeverity.Critical => 25,
                IssueSeverity.High => 15,
                IssueSeverity.Medium => 5,
                IssueSeverity.Low => 2,
                _ => 0
            };
        }
        
        var failRate = session.Steps.Count > 0
            ? (double)session.Steps.Count(s => !s.Validation.Passed) / session.Steps.Count
            : 0;
        baseScore -= failRate * 20;
        
        if (session.GoalAchieved)
            baseScore += 10;
        
        return Math.Max(0, Math.Min(100, baseScore));
    }
    
    private async Task<(List<string> Findings, List<string> Recommendations)> GenerateAiSummaryAsync(
        TestSession session, IExecutionContext context, CancellationToken ct)
    {
        if (_providerFactory == null) return (new List<string>(), new List<string>());
        
        var prompt = $@"Summarize this test session:

Goal: {session.Goal}
Duration: {(session.EndTime - session.StartTime).TotalMinutes:F1} minutes
Steps: {session.Steps.Count}
Issues: {session.AllIssues.Count} ({session.AllIssues.Count(i => i.Severity >= IssueSeverity.High)} high/critical)

Provide JSON: {{ ""findings"": [], ""recommendations"": [] }}";

        try
        {
            var response = await _providerFactory.ExecuteLLMAsync(
                context.Provider,
                "You are generating a test report summary.",
                prompt,
                new { },
                ct);
            
            var doc = JsonDocument.Parse(response);
            
            return (
                doc.RootElement.GetProperty("findings").EnumerateArray().Select(f => f.GetString()!).ToList(),
                doc.RootElement.GetProperty("recommendations").EnumerateArray().Select(r => r.GetString()!).ToList()
            );
        }
        catch
        {
            return (new List<string>(), new List<string>());
        }
    }
    
    private static string GenerateHtmlReport(TestSession session, TestSummary summary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><title>Test Report</title></head><body>");
        sb.AppendLine($"<h1>Test Report</h1>");
        sb.AppendLine($"<h2>Summary</h2>");
        sb.AppendLine($"<p>Score: {summary.OverallScore:F0}/100</p>");
        sb.AppendLine($"<p>Passed: {summary.Passed}/{summary.TotalTests}</p>");
        sb.AppendLine($"<p>Duration: {summary.Duration.TotalMinutes:F1} minutes</p>");
        sb.AppendLine($"<h2>Key Findings</h2><ul>");
        foreach (var finding in summary.KeyFindings)
            sb.AppendLine($"<li>{finding}</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
