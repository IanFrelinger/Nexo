using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Execution;
using Nexo.Core.Domain.Execution.Events;

namespace Nexo.Infrastructure.Execution;

/// <summary>
/// Executes behaviors by running their brick steps in sequence.
/// </summary>
public class BehaviorExecutor : IBehaviorExecutor
{
    private readonly IBrickRegistry _brickRegistry;
    private readonly IProviderFactory _providerFactory;
    private readonly ISemanticCache _semanticCache;
    private readonly ILogger<BehaviorExecutor> _logger;
    
    public BehaviorExecutor(
        IBrickRegistry brickRegistry,
        IProviderFactory providerFactory,
        ISemanticCache semanticCache,
        ILogger<BehaviorExecutor> logger)
    {
        _brickRegistry = brickRegistry;
        _providerFactory = providerFactory;
        _semanticCache = semanticCache;
        _logger = logger;
    }
    
    public async Task<BehaviorResult> ExecuteAsync(
        AgentCard agent,
        Behavior behavior,
        BehaviorInput input,
        ExecutionOptions options,
        CancellationToken cancellationToken = default)
    {
        var outputs = new Dictionary<string, object>();
        var errors = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        
        await foreach (var evt in ExecuteWithEventsAsync(agent, behavior, input, options, cancellationToken))
        {
            if (evt is BehaviorCompletedEvent completed)
            {
                outputs = new Dictionary<string, object>(completed.Outputs);
            }
            else if (evt is StepErrorEvent error)
            {
                errors.Add($"{error.StepId}: {error.Error}");
            }
        }
        
        stopwatch.Stop();
        
        return new BehaviorResult
        {
            Success = errors.Count == 0,
            Outputs = outputs,
            Errors = errors,
            Duration = stopwatch.Elapsed
        };
    }
    
    public async IAsyncEnumerable<ExecutionEvent> ExecuteWithEventsAsync(
        AgentCard agent,
        Behavior behavior,
        BehaviorInput input,
        ExecutionOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = new ExecutionContext
        {
            AgentId = agent.Id,
            BehaviorId = behavior.Id,
            IsAirGapped = options.IsAirGapped,
            AuditMode = options.AuditMode,
            Provider = options.Provider,
            Variables = new Dictionary<string, object>(input.Parameters)
        };
        
        yield return new BehaviorStartedEvent(behavior.Id, behavior.Name, DateTime.UtcNow);
        
        var stepIndex = 0;
        foreach (var step in behavior.Steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield return new BehaviorCancelledEvent(behavior.Id);
                yield break;
            }
            
            // Check step condition
            if (step.Condition != null && !EvaluateCondition(step.Condition, context))
            {
                yield return new StepSkippedEvent(step.Id, step.Condition);
                continue;
            }
            
            var brick = _brickRegistry.GetBrick(step.BrickId);
            if (brick == null)
            {
                yield return new StepErrorEvent(step.Id, $"Brick not found: {step.BrickId}");
                if (behavior.OnStepFailure == FailurePolicy.Abort)
                    yield break;
                continue;
            }
            
            // Determine implementation
            var implementation = ResolveImplementation(step, brick, context);
            var usedFallback = false;
            
            // Check if implementation is available
            if (!IsImplementationAvailable(brick, implementation, context))
            {
                var fallback = GetFallbackImplementation(brick, context);
                if (fallback == null)
                {
                    yield return new StepErrorEvent(step.Id, "No available implementation");
                    if (behavior.OnStepFailure == FailurePolicy.Abort)
                        yield break;
                    continue;
                }
                implementation = fallback.Value;
                usedFallback = true;
            }
            
            yield return new StepStartedEvent(
                step.Id, 
                brick.Id, 
                brick.Name, 
                implementation,
                usedFallback,
                stepIndex,
                behavior.Steps.Count);
            
            var stopwatch = Stopwatch.StartNew();
            
            // Map inputs
            var brickInput = MapInputs(step.InputMapping, context);
            
            // Check semantic cache
            var cacheKey = ComputeCacheKey(brick.Id, brickInput, implementation);
            var cached = await _semanticCache.GetAsync(cacheKey, cancellationToken);
            
            BrickOutput? output = null;
            string? error = null;
            
            if (cached != null)
            {
                output = cached;
                yield return new CacheHitEvent(step.Id, cacheKey);
            }
            else
            {
                try
                {
                    // Execute brick
                    output = await brick.ExecuteAsync(brickInput, implementation, context, cancellationToken);
                    
                    // Cache result
                    await _semanticCache.SetAsync(cacheKey, output, cancellationToken);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    _logger.LogError(ex, "Error executing step {StepId}", step.Id);
                }
            }
            
            stopwatch.Stop();
            
            if (error != null)
            {
                yield return new StepErrorEvent(step.Id, error, stopwatch.ElapsedMilliseconds);
                
                if (behavior.OnStepFailure == FailurePolicy.Abort)
                    yield break;
            }
            else if (output != null)
            {
                // Map outputs to context
                MapOutputs(step.OutputMapping, output, context);
                
                yield return new StepCompletedEvent(
                    step.Id,
                    brick.Id,
                    implementation,
                    stopwatch.ElapsedMilliseconds,
                    output.Summary);
            }
            
            stepIndex++;
        }
        
        // Evaluate success criteria
        var success = EvaluateSuccessCriteria(behavior.SuccessCriteria, context);
        
        yield return new BehaviorCompletedEvent(
            behavior.Id,
            success,
            context.Variables);
    }
    
    private ImplementationType ResolveImplementation(
        BehaviorStep step, 
        Brick brick, 
        ExecutionContext context)
    {
        if (step.Implementation != ImplementationType.Auto)
            return step.Implementation;
        
        if (brick.Selector != null)
            return brick.Selector.Select(context);
        
        return brick.DefaultImplementation;
    }
    
    private bool IsImplementationAvailable(
        Brick brick, 
        ImplementationType implementation, 
        ExecutionContext context)
    {
        return implementation switch
        {
            ImplementationType.Deterministic => brick.Implementations.HasDeterministic,
            ImplementationType.Agentic => brick.Implementations.HasAgentic && 
                                          !context.IsAirGapped &&
                                          _providerFactory.IsProviderAvailable(context.Provider),
            _ => false
        };
    }
    
    private ImplementationType? GetFallbackImplementation(Brick brick, ExecutionContext context)
    {
        foreach (var fallback in brick.FallbackChain)
        {
            if (IsImplementationAvailable(brick, fallback, context))
                return fallback;
        }
        return null;
    }
    
    private static bool EvaluateCondition(string condition, ExecutionContext context)
    {
        // Simple condition evaluation - extend as needed
        // For now, just check for simple boolean expressions
        return condition.Trim().ToLowerInvariant() switch
        {
            "true" => true,
            "false" => false,
            _ => false
        };
    }
    
    private static BrickInput MapInputs(
        IReadOnlyDictionary<string, string> inputMapping,
        ExecutionContext context)
    {
        var brickInput = new BrickInput();
        
        foreach (var mapping in inputMapping)
        {
            // Simple mapping: evaluate expression or use variable
            if (context.Variables.TryGetValue(mapping.Value, out var value))
            {
                brickInput.Set(mapping.Key, value);
            }
            else
            {
                // Try to parse as literal
                brickInput.Set(mapping.Key, mapping.Value);
            }
        }
        
        return brickInput;
    }
    
    private static void MapOutputs(
        IReadOnlyDictionary<string, string> outputMapping,
        BrickOutput output,
        ExecutionContext context)
    {
        foreach (var mapping in outputMapping)
        {
            // Simple mapping: get from output
            if (output.ToDictionary().TryGetValue(mapping.Value, out var value))
            {
                context.Variables[mapping.Key] = value;
            }
        }
    }
    
    private static string ComputeCacheKey(string brickId, BrickInput input, ImplementationType implementation)
    {
        // Simple cache key - in production, use semantic hashing
        var inputHash = System.Text.Json.JsonSerializer.Serialize(input.ToDictionary())
            .GetHashCode();
        return $"{brickId}:{implementation}:{inputHash}";
    }
    
    private static bool EvaluateSuccessCriteria(SuccessCriteria criteria, ExecutionContext context)
    {
        if (criteria.AllStepsComplete)
        {
            // This is evaluated by checking if we completed all steps
            // For now, assume success if we got here
            return true;
        }
        
        // Additional validation logic can be added here
        return true;
    }
}

