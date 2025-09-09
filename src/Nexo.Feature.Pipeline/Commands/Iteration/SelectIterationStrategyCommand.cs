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

namespace Nexo.Feature.Pipeline.Commands.Iteration;

/// <summary>
/// Pipeline command for selecting optimal iteration strategies
/// </summary>
[Command("iteration.select-strategy")]
public class SelectIterationStrategyCommand : ICommand<SelectIterationStrategyRequest, SelectIterationStrategyResponse>
{
    private readonly IIterationStrategySelector _strategySelector;
    private readonly ILogger<SelectIterationStrategyCommand> _logger;
    
    public SelectIterationStrategyCommand(
        IIterationStrategySelector strategySelector,
        ILogger<SelectIterationStrategyCommand> logger)
    {
        _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<SelectIterationStrategyResponse> ExecuteAsync(
        SelectIterationStrategyRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Selecting iteration strategy for data size {DataSize} on platform {Platform}", 
                request.EstimatedDataSize, request.EnvironmentProfile?.PlatformType);
            
            var context = new IterationContext
            {
                DataSize = request.EstimatedDataSize,
                Requirements = ConvertToIterationRequirements(request.Requirements),
                EnvironmentProfile = request.EnvironmentProfile ?? RuntimeEnvironmentDetector.DetectCurrent(),
                PipelineContext = request.PipelineContext,
                TargetPlatform = request.TargetPlatform,
                IsCpuBound = request.IsCpuBound,
                IsIoBound = request.IsIoBound,
                RequiresAsync = request.RequiresAsync
            };
            
            var strategy = _strategySelector.SelectStrategy<object>(context);
            var reasoning = _strategySelector.GetSelectionReasoning(context);
            var performanceEstimate = _strategySelector.EstimatePerformance(strategy, context);
            
            _logger.LogInformation("Selected iteration strategy {StrategyId} with performance score {Score}", 
                strategy.StrategyId, performanceEstimate.PerformanceScore);
            
            return new SelectIterationStrategyResponse
            {
                SelectedStrategy = strategy,
                SelectionReasoning = reasoning,
                PerformanceEstimate = performanceEstimate,
                Context = context,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting iteration strategy");
            return new SelectIterationStrategyResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    /// <summary>
    /// Converts PerformanceRequirements to IterationRequirements
    /// </summary>
    private static IterationRequirements ConvertToIterationRequirements(Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements performanceRequirements)
    {
        return new IterationRequirements
        {
            PrioritizeCpu = performanceRequirements.RequiresRealTime,
            PrioritizeMemory = performanceRequirements.MemoryCritical,
            RequiresParallelization = performanceRequirements.PreferParallel,
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            Timeout = TimeSpan.FromMilliseconds(performanceRequirements.MaxExecutionTimeMs)
        };
    }
}

/// <summary>
/// Request for selecting iteration strategy
/// </summary>
public record SelectIterationStrategyRequest
{
    /// <summary>
    /// Estimated data size for iteration
    /// </summary>
    public int EstimatedDataSize { get; init; }
    
    /// <summary>
    /// Performance requirements
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceRequirements Requirements { get; init; } = new();
    
    /// <summary>
    /// Runtime environment profile
    /// </summary>
    public RuntimeEnvironmentProfile? EnvironmentProfile { get; init; }
    
    /// <summary>
    /// Pipeline context
    /// </summary>
    public PipelineContext PipelineContext { get; init; } = new();
    
    /// <summary>
    /// Target platform
    /// </summary>
    public PlatformTarget TargetPlatform { get; init; } = PlatformTarget.DotNet;
    
    /// <summary>
    /// Whether the operation is CPU-bound
    /// </summary>
    public bool IsCpuBound { get; init; } = false;
    
    /// <summary>
    /// Whether the operation is I/O-bound
    /// </summary>
    public bool IsIoBound { get; init; } = false;
    
    /// <summary>
    /// Whether async processing is required
    /// </summary>
    public bool RequiresAsync { get; init; } = false;
}

/// <summary>
/// Response from iteration strategy selection
/// </summary>
public record SelectIterationStrategyResponse
{
    /// <summary>
    /// Selected iteration strategy
    /// </summary>
    public IIterationStrategy<object>? SelectedStrategy { get; init; }
    
    /// <summary>
    /// Reasoning for strategy selection
    /// </summary>
    public string SelectionReasoning { get; init; } = string.Empty;
    
    /// <summary>
    /// Performance estimate for the selected strategy
    /// </summary>
    public Nexo.Core.Domain.Entities.Infrastructure.PerformanceEstimate PerformanceEstimate { get; init; } = new();
    
    /// <summary>
    /// Iteration context used for selection
    /// </summary>
    public IterationContext? Context { get; init; }
    
    /// <summary>
    /// Whether the selection was successful
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Error message if selection failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}
