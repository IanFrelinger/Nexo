using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Bricks;
using Nexo.Agents.UniversalTester.Configuration;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using System.Text.Json;

namespace Nexo.Agents.UniversalTester;

/// <summary>
/// Universal Testing Agent that can test any application - web, games, desktop, APIs, CLIs.
/// Uses AI to understand and interact with applications.
/// </summary>
public class UniversalTesterAgent
{
    private readonly IProviderFactory _providerFactory;
    private readonly ILogger<UniversalTesterAgent>? _logger;
    
    public UniversalTesterAgent(IProviderFactory providerFactory, ILogger<UniversalTesterAgent>? logger = null)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }
    
    /// <summary>
    /// Executes a test session based on the configuration.
    /// </summary>
    public async Task<TestReport> ExecuteAsync(
        UniversalTesterConfig config,
        IExecutionContext context,
        CancellationToken ct = default)
    {
        _logger?.LogInformation("Starting universal test session");
        _logger?.LogInformation("Target: {Target}", config.Target);
        _logger?.LogInformation("Goal: {Goal}", config.Goal);
        
        // Determine target type
        var targetType = config.TargetType ?? InferTargetType(config.Target);
        
        // Create appropriate adapter
        await using var adapter = CreateAdapter(targetType);
        await adapter.ConnectAsync(config.Target, ct);
        
        // Initialize bricks - create logger factory if needed
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var perceptionBrick = new PerceptionBrick(loggerFactory.CreateLogger<PerceptionBrick>());
        var understandingBrick = new UnderstandingBrick(_providerFactory, loggerFactory.CreateLogger<UnderstandingBrick>());
        var explorationBrick = new ExplorationBrick(_providerFactory, loggerFactory.CreateLogger<ExplorationBrick>());
        var actionBrick = new ActionExecutorBrick(loggerFactory.CreateLogger<ActionExecutorBrick>());
        var validationBrick = new ValidationBrick(_providerFactory, loggerFactory.CreateLogger<ValidationBrick>());
        var reportingBrick = new ReportingBrick(_providerFactory, loggerFactory.CreateLogger<ReportingBrick>());
        
        // Test session state
        var session = new TestSession
        {
            Goal = config.Goal,
            TargetDescription = $"{targetType}: {config.Target}",
            StartTime = DateTime.UtcNow
        };
        
        var steps = new List<TestStep>();
        var allIssues = new List<DetectedIssue>();
        var actionHistory = new List<TestAction>();
        PerceptionState? previousPerception = null;
        Understanding? previousUnderstanding = null;
        var startTime = DateTime.UtcNow;
        
        // Main test loop
        var stepNumber = 0;
        while (DateTime.UtcNow - startTime < config.MaxDuration && !ct.IsCancellationRequested)
        {
            stepNumber++;
            _logger?.LogDebug("Step {Step}", stepNumber);
            
            // 1. PERCEIVE
            var perceptionInput = new BrickInput();
            perceptionInput.Set("adapter", adapter);
            if (previousPerception != null)
                perceptionInput.Set("previousState", previousPerception);
            perceptionInput.Set("captureScreenshot", true);
            perceptionInput.Set("captureDOM", true);
            perceptionInput.Set("capturePerformance", true);
            
            var perceptionOutput = await perceptionBrick.ExecuteAsync(
                perceptionInput,
                ImplementationType.Deterministic,
                context,
                ct);
            
            var perception = perceptionOutput.Get<PerceptionState>("perception");
            
            // 2. UNDERSTAND
            var understandingInput = new BrickInput();
            understandingInput.Set("perception", perception);
            understandingInput.Set("goal", config.Goal);
            understandingInput.Set("constraints", config.Constraints ?? Array.Empty<string>());
            understandingInput.Set("actionHistory", actionHistory.ToArray());
            if (previousUnderstanding != null)
                understandingInput.Set("previousUnderstanding", previousUnderstanding);
            
            var understandingOutput = await understandingBrick.ExecuteAsync(
                understandingInput,
                ImplementationType.Agentic,
                context,
                ct);
            
            var understanding = understandingOutput.Get<Understanding>("understanding");
            
            // Collect any issues found during understanding
            allIssues.AddRange(understanding.Issues);
            
            // 3. DECIDE (Exploration)
            var explorationInput = new BrickInput();
            explorationInput.Set("understanding", understanding);
            explorationInput.Set("goal", config.Goal);
            explorationInput.Set("depth", config.Depth);
            explorationInput.Set("actionHistory", actionHistory.ToArray());
            
            var explorationOutput = await explorationBrick.ExecuteAsync(
                explorationInput,
                ImplementationType.Agentic,
                context,
                ct);
            
            var nextAction = explorationOutput.Get<TestAction>("nextAction");
            var shouldStop = explorationOutput.Get<bool>("shouldStop");
            
            // Check if we should stop
            if (shouldStop)
            {
                _logger?.LogInformation("Stopping: Goal achieved or stuck");
                session = session with
                {
                    StopReason = "Goal achieved or exploration complete",
                    GoalAchieved = understanding.ProgressPercent >= 90
                };
                break;
            }
            
            actionHistory.Add(nextAction);
            
            // 4. ACT
            var actionInput = new BrickInput();
            actionInput.Set("action", nextAction);
            actionInput.Set("adapter", adapter);
            
            var actionOutput = await actionBrick.ExecuteAsync(
                actionInput,
                ImplementationType.Deterministic,
                context,
                ct);
            
            var executionResult = actionOutput.Get<ActionExecutionResult>("result");
            
            // 5. VALIDATE
            var validationInput = new BrickInput();
            validationInput.Set("action", nextAction);
            validationInput.Set("executionResult", executionResult);
            validationInput.Set("goal", config.Goal);
            if (!string.IsNullOrEmpty(nextAction.ExpectedOutcome))
                validationInput.Set("expectedOutcome", nextAction.ExpectedOutcome);
            
            var validationOutput = await validationBrick.ExecuteAsync(
                validationInput,
                ImplementationType.Agentic,
                context,
                ct);
            
            var validation = validationOutput.Get<ValidationResult>("validation");
            
            allIssues.AddRange(validation.IssuesFound);
            
            // Record step
            steps.Add(new TestStep
            {
                StepNumber = stepNumber,
                Action = nextAction,
                ExecutionResult = executionResult,
                Validation = validation,
                UnderstandingBefore = understanding
            });
            
            // Update state for next iteration
            previousPerception = perception;
            previousUnderstanding = understanding;
        }
        
        // 6. REPORT
        session = session with
        {
            EndTime = DateTime.UtcNow,
            Steps = steps,
            AllIssues = allIssues,
            ProgressPercent = previousUnderstanding?.ProgressPercent ?? 0
        };
        
        var reportingInput = new BrickInput();
        reportingInput.Set("session", session);
        reportingInput.Set("format", "html");
        
        var reportOutput = await reportingBrick.ExecuteAsync(
            reportingInput,
            ImplementationType.Agentic,
            context,
            ct);
        
        var report = reportOutput.Get<TestReport>("report");
        
        _logger?.LogInformation("Testing complete. Score: {Score:F0}, Issues: {Issues}",
            report.Summary.OverallScore, allIssues.Count);
        
        return report ?? new TestReport
        {
            Summary = new TestSummary
            {
                TotalTests = 0,
                Passed = 0,
                Failed = 0,
                Warnings = 0,
                Duration = TimeSpan.Zero,
                OverallScore = 0
            }
        };
    }
    
    private static TargetType InferTargetType(string target)
    {
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return TargetType.WebApp;
        if (target.StartsWith("api://", StringComparison.OrdinalIgnoreCase))
            return TargetType.Api;
        if (target.StartsWith("cli://", StringComparison.OrdinalIgnoreCase))
            return TargetType.Cli;
        if (target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || 
            target.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            return target.Contains("Unity", StringComparison.OrdinalIgnoreCase) || 
                   target.Contains("Game", StringComparison.OrdinalIgnoreCase)
                ? TargetType.Game
                : TargetType.DesktopApp;
        
        return TargetType.WebApp; // Default
    }
    
    private ITargetAdapter CreateAdapter(TargetType type)
    {
        // In a real implementation, these would be injected via DI
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        return type switch
        {
            TargetType.WebApp => new WebAdapter(loggerFactory.CreateLogger<WebAdapter>()),
            TargetType.Game => new GameAdapter(loggerFactory.CreateLogger<GameAdapter>()),
            TargetType.Api => new ApiAdapter(loggerFactory.CreateLogger<ApiAdapter>()),
            TargetType.Cli => new CliAdapter(loggerFactory.CreateLogger<CliAdapter>()),
            TargetType.DesktopApp => new DesktopAdapter(loggerFactory.CreateLogger<DesktopAdapter>()),
            _ => new WebAdapter(loggerFactory.CreateLogger<WebAdapter>())
        };
    }
}
