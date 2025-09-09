using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;
using Nexo.Core.Domain.Entities.Infrastructure;
using Nexo.Core.Domain.Interfaces.Infrastructure;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Behaviors.Iteration;

/// <summary>
/// Pipeline behavior that automatically optimizes iteration operations
/// </summary>
[Behavior("iteration.optimization")]
public class IterationOptimizationBehavior : IPipelineBehavior
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly ILogger<IterationOptimizationBehavior> _logger;
    
    public IterationOptimizationBehavior(
        IIterationStrategySelector strategySelector,
        ILogger<IterationOptimizationBehavior> logger)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<TResponse> Handle<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if request involves iteration operations
            if (request is IIterationAwareRequest iterationRequest)
            {
                _logger.LogDebug("Processing iteration-aware request");
                
                // Get iteration context from the request
                var iterationContext = iterationRequest.GetIterationContext();
                
                // Select optimal strategy for this context
                var strategy = _strategySelector.SelectStrategy<object>(iterationContext);
                
                // Add strategy information to pipeline context
                var pipelineContext = PipelineContext.Current;
                pipelineContext.SetProperty("SelectedIterationStrategy", strategy);
                pipelineContext.SetProperty("IterationOptimized", true);
                pipelineContext.SetProperty("IterationContext", iterationContext);
                
                _logger.LogInformation("Selected iteration strategy {StrategyId} for request", strategy.StrategyId);
            }
            
            // Continue with the pipeline
            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in iteration optimization behavior");
            // Don't fail the pipeline if iteration optimization fails
            return await next();
        }
    }
}

/// <summary>
/// Interface for requests that are aware of iteration operations
/// </summary>
public interface IIterationAwareRequest
{
    /// <summary>
    /// Get the iteration context for this request
    /// </summary>
    /// <returns>Iteration context</returns>
    IterationContext GetIterationContext();
}

/// <summary>
/// Base class for iteration-aware requests
/// </summary>
public abstract class IterationAwareRequest : IIterationAwareRequest
{
    /// <summary>
    /// Estimated data size for iteration
    /// </summary>
    public int EstimatedDataSize { get; set; } = 1000;
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements PerformanceRequirements { get; set; } = new();
    
    /// <summary>
    /// Target platform
    /// </summary>
    public PlatformTarget TargetPlatform { get; set; } = PlatformTarget.DotNet;
    
    /// <summary>
    /// Whether the operation is CPU-bound
    /// </summary>
    public bool IsCpuBound { get; set; } = false;
    
    /// <summary>
    /// Whether the operation is I/O-bound
    /// </summary>
    public bool IsIoBound { get; set; } = false;
    
    /// <summary>
    /// Whether async processing is required
    /// </summary>
    public bool RequiresAsync { get; set; } = false;
    
    /// <summary>
    /// Runtime environment profile
    /// </summary>
    public RuntimeEnvironmentProfile? EnvironmentProfile { get; set; }
    
    public virtual IterationContext GetIterationContext()
    {
        return new IterationContext
        {
            DataSize = EstimatedDataSize,
            Requirements = PerformanceRequirements,
            EnvironmentProfile = EnvironmentProfile ?? RuntimeEnvironmentDetector.DetectCurrent(),
            PipelineContext = PipelineContext.Current,
            TargetPlatform = TargetPlatform,
            IsCpuBound = IsCpuBound,
            IsIoBound = IsIoBound,
            RequiresAsync = RequiresAsync
        };
    }
}

/// <summary>
/// Iteration optimization context for pipeline
/// </summary>
public static class IterationOptimizationContext
{
    /// <summary>
    /// Get the selected iteration strategy from pipeline context
    /// </summary>
    /// <param name="context">Pipeline context</param>
    /// <returns>Selected iteration strategy or null</returns>
    public static IIterationStrategy<object>? GetSelectedStrategy(PipelineContext context)
    {
        return context.GetProperty<IIterationStrategy<object>>("SelectedIterationStrategy");
    }
    
    /// <summary>
    /// Check if iteration optimization was applied
    /// </summary>
    /// <param name="context">Pipeline context</param>
    /// <returns>True if iteration optimization was applied</returns>
    public static bool IsIterationOptimized(PipelineContext context)
    {
        return context.GetProperty<bool>("IterationOptimized");
    }
    
    /// <summary>
    /// Get the iteration context from pipeline context
    /// </summary>
    /// <param name="context">Pipeline context</param>
    /// <returns>Iteration context or null</returns>
    public static IterationContext? GetIterationContext(PipelineContext context)
    {
        return context.GetProperty<IterationContext>("IterationContext");
    }
    
    /// <summary>
    /// Set iteration optimization information in pipeline context
    /// </summary>
    /// <param name="context">Pipeline context</param>
    /// <param name="strategy">Selected strategy</param>
    /// <param name="iterationContext">Iteration context</param>
    public static void SetIterationOptimization(
        PipelineContext context, 
        IIterationStrategy<object> strategy, 
        IterationContext iterationContext)
    {
        context.SetProperty("SelectedIterationStrategy", strategy);
        context.SetProperty("IterationOptimized", true);
        context.SetProperty("IterationContext", iterationContext);
    }
}
