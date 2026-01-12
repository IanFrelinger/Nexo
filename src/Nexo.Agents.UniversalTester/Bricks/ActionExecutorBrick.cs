using Microsoft.Extensions.Logging;
using Nexo.Agents.UniversalTester.Adapters;
using Nexo.Agents.UniversalTester.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Agents.UniversalTester.Bricks;

/// <summary>
/// Executes actions on the target application.
/// </summary>
public class ActionExecutorBrick : Brick
{
    private readonly ILogger<ActionExecutorBrick>? _logger;
    
    public ActionExecutorBrick(ILogger<ActionExecutorBrick>? logger = null)
    {
        _logger = logger;
        
        Id = "universal-tester.action-executor";
        Name = "Action Executor";
        Version = "1.0.0";
        Icon = "⚡";
        Category = BrickCategory.Control;
        Description = "Executes actions on the target application";
        
        Interface = new BrickInterface
        {
            Inputs = [
                new BrickInputDefinition("action", "TestAction", "Action to execute"),
                new BrickInputDefinition("adapter", "ITargetAdapter", "Target adapter")
            ],
            Outputs = [
                new BrickOutputDefinition("result", "ActionExecutionResult", "Execution result")
            ]
        };
        
        Implementations = new BrickImplementations
        {
            Deterministic = new DeterministicImplementation
            {
                Id = "action-executor-deterministic",
                Name = "Action Executor",
                Description = "Executes actions via adapter",
                Executor = "DirectExecutor",
                Config = new Dictionary<string, object>()
            }
        };
        
        DefaultImplementation = ImplementationType.Deterministic;
    }
    
    public override async Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var action = input.Get<TestAction>("action");
        var adapter = input.Get<ITargetAdapter>("adapter");
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        byte[]? screenshotBefore = null;
        byte[]? screenshotAfter = null;
        string? error = null;
        string? result = null;
        
        try
        {
            screenshotBefore = await adapter.CaptureScreenshotAsync(cancellationToken);
            
            result = await adapter.ExecuteActionAsync(action, cancellationToken);
            
            await Task.Delay(200, cancellationToken); // Wait for animations
            
            screenshotAfter = await adapter.CaptureScreenshotAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            _logger?.LogWarning(ex, "Action {ActionId} failed: {Error}", action.Id, ex.Message);
        }
        
        sw.Stop();
        
        var executionResult = new ActionExecutionResult
        {
            Success = error == null,
            Duration = sw.Elapsed,
            Error = error,
            ScreenshotBefore = screenshotBefore,
            ScreenshotAfter = screenshotAfter,
            ResultDescription = result
        };
        
        return new BrickOutput
        {
            ["result"] = executionResult,
            Summary = error == null ? $"Executed: {action.Description}" : $"Failed: {error}"
        };
    }
}
