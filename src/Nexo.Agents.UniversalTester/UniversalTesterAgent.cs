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
    public Task<TestReport> ExecuteAsync(
        UniversalTesterConfig config,
        IExecutionContext context,
        CancellationToken ct = default)
        => ExecuteAsync(config, context, runtime: null, ct);

    /// <summary>
    /// Executes a test session based on the configuration, with a runtime config that controls
    /// per-brick implementation selection and fallback.
    /// </summary>
    public async Task<TestReport> ExecuteAsync(
        UniversalTesterConfig config,
        IExecutionContext context,
        UniversalTesterRuntimeConfig? runtime = null,
        CancellationToken ct = default)
    {
        _logger?.LogInformation("Starting universal test session");
        _logger?.LogInformation("Target: {Target}", config.Target);
        _logger?.LogInformation("Goal: {Goal}", config.Goal);
        
        // Determine target type
        var targetType = config.TargetType ?? InferTargetType(config.Target);

        var provider = (context.Provider ?? "").Trim().ToLowerInvariant();
        if (provider is "ollama" or "auto" or "local")
        {
            var requireVision = targetType == TargetType.DesktopApp;
            await _providerFactory.EnsureOllamaReachableAsync(requireVision, ct);
        }

        // Create appropriate adapter
        await using var adapter = CreateAdapter(targetType);
        await adapter.ConnectAsync(config.Target, ct);
        
        // Initialize bricks - create logger factory if needed
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        var perceptionBrick = new PerceptionBrick(_providerFactory, loggerFactory.CreateLogger<PerceptionBrick>());
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
        var frameBuffer = new List<byte[]>();
        var startTime = DateTime.UtcNow;
        
        // Main test loop
        var stepNumber = 0;
        runtime ??= UniversalTesterRuntimeConfig.Default();

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
            
            var perceptionOutput = await ExecuteBrickWithRuntimeFallbackAsync(
                brickKey: "perception",
                brick: perceptionBrick,
                input: perceptionInput,
                context: context,
                runtime: runtime,
                validate: o => o.Get<PerceptionState>("perception") != null,
                ct: ct);
            
            var perception = perceptionOutput.Get<PerceptionState>("perception");

            // Maintain rolling frame buffer for multi-frame vision
            if (perception?.Screenshot is { Length: > 0 } screenshot)
            {
                frameBuffer.Add(screenshot);
                var maxFrames = Math.Max(1, runtime.MultiFrameCount);
                while (frameBuffer.Count > maxFrames)
                    frameBuffer.RemoveAt(0);
            }
            
            // 2. UNDERSTAND
            var understandingInput = new BrickInput();
            understandingInput.Set("perception", perception!);
            understandingInput.Set("goal", config.Goal ?? "");
            understandingInput.Set("constraints", config.Constraints ?? Array.Empty<string>());
            understandingInput.Set("actionHistory", actionHistory.ToArray());
            if (previousUnderstanding != null)
                understandingInput.Set("previousUnderstanding", previousUnderstanding);
            if (frameBuffer.Count > 0)
                understandingInput.Set("recentScreenshots", frameBuffer.ToList());

            var understandingOutput = await ExecuteBrickWithRuntimeFallbackAsync(
                brickKey: "understanding",
                brick: understandingBrick,
                input: understandingInput,
                context: context,
                runtime: runtime,
                validate: o => o.Get<Understanding>("understanding") != null,
                ct: ct);
            
            var understanding = understandingOutput.Get<Understanding>("understanding");
            
            // Collect any issues found during understanding
            allIssues.AddRange(understanding.Issues);
            
            // 3. DECIDE (Exploration)
            var explorationInput = new BrickInput();
            explorationInput.Set("understanding", understanding);
            explorationInput.Set("goal", config.Goal ?? "");
            explorationInput.Set("depth", config.Depth);
            explorationInput.Set("actionHistory", actionHistory.ToArray());
            
            var explorationOutput = await ExecuteBrickWithRuntimeFallbackAsync(
                brickKey: "exploration",
                brick: explorationBrick,
                input: explorationInput,
                context: context,
                runtime: runtime,
                validate: o => o.Get<TestAction>("nextAction") != null,
                ct: ct);
            
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
            
            var actionOutput = await ExecuteBrickWithRuntimeFallbackAsync(
                brickKey: "action",
                brick: actionBrick,
                input: actionInput,
                context: context,
                runtime: runtime,
                validate: o => o.Get<ActionExecutionResult>("result") != null,
                ct: ct);
            
            var executionResult = actionOutput.Get<ActionExecutionResult>("result");
            
            // 5. VALIDATE
            var validationInput = new BrickInput();
            validationInput.Set("action", nextAction);
            validationInput.Set("executionResult", executionResult);
            validationInput.Set("goal", config.Goal ?? "");
            if (!string.IsNullOrEmpty(nextAction.ExpectedOutcome))
                validationInput.Set("expectedOutcome", nextAction.ExpectedOutcome);
            
            var validationOutput = await ExecuteBrickWithRuntimeFallbackAsync(
                brickKey: "validation",
                brick: validationBrick,
                input: validationInput,
                context: context,
                runtime: runtime,
                validate: o => o.Get<ValidationResult>("validation") != null,
                ct: ct);
            
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
        
        var reportOutput = await ExecuteBrickWithRuntimeFallbackAsync(
            brickKey: "reporting",
            brick: reportingBrick,
            input: reportingInput,
            context: context,
            runtime: runtime,
            validate: o => o.Get<TestReport>("report") != null,
            ct: ct);
        
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

    private async Task<BrickOutput> ExecuteBrickWithRuntimeFallbackAsync(
        string brickKey,
        Brick brick,
        BrickInput input,
        IExecutionContext context,
        UniversalTesterRuntimeConfig runtime,
        Func<BrickOutput, bool> validate,
        CancellationToken ct)
    {
        var implsToTry = ResolveImplementationChain(brickKey, brick, context, runtime);
        Exception? last = null;

        foreach (var impl in implsToTry)
        {
            try
            {
                _logger?.LogDebug("Executing brick {BrickKey} with {Implementation}", brickKey, impl);
                var output = await brick.ExecuteAsync(input, impl, context, ct);
                if (!validate(output))
                {
                    throw new InvalidOperationException($"Brick '{brickKey}' output failed validation for implementation {impl}");
                }
                return output;
            }
            catch (Exception ex)
            {
                last = ex;
                _logger?.LogWarning(ex, "Brick {BrickKey} failed with {Implementation}; trying fallback if available", brickKey, impl);
            }
        }

        throw last ?? new InvalidOperationException($"Brick '{brickKey}' failed and no fallback implementations were available");
    }

    private IReadOnlyList<ImplementationType> ResolveImplementationChain(
        string brickKey,
        Brick brick,
        IExecutionContext context,
        UniversalTesterRuntimeConfig runtime)
    {
        // In air-gapped mode we force deterministic (unless caller explicitly supplies only agentic, which would fail anyway).
        if (context.IsAirGapped)
        {
            return new[] { ImplementationType.Deterministic };
        }

        if (!runtime.Bricks.TryGetValue(brickKey, out var spec))
        {
            spec = new BrickRuntimeSpec { Prefer = runtime.Prefer, Fallback = brick.FallbackChain.ToList() };
        }

        var prefer = (spec.Prefer ?? runtime.Prefer ?? "auto").Trim().ToLowerInvariant();

        // Start with preferred if explicit; otherwise use brick default.
        var first = prefer switch
        {
            "deterministic" => ImplementationType.Deterministic,
            "agentic" => ImplementationType.Agentic,
            _ => brick.DefaultImplementation
        };

        // Compose chain: preferred first, then runtime fallback (or brick fallback).
        var chain = new List<ImplementationType> { first };
        foreach (var f in (spec.Fallback?.Count > 0 ? spec.Fallback : brick.FallbackChain))
        {
            if (!chain.Contains(f)) chain.Add(f);
        }

        // Filter chain for availability in this environment (provider config).
        return chain
            .Where(t => IsImplementationAvailable(brick, t, context))
            .ToList();
    }

    private bool IsImplementationAvailable(Brick brick, ImplementationType implementation, IExecutionContext context)
    {
        return implementation switch
        {
            ImplementationType.Deterministic => brick.Implementations.HasDeterministic,
            ImplementationType.Agentic => brick.Implementations.HasAgentic && _providerFactory.IsProviderAvailable(context.Provider),
            _ => false
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
        if (target.StartsWith("process://", StringComparison.OrdinalIgnoreCase))
            return TargetType.DesktopApp;
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
