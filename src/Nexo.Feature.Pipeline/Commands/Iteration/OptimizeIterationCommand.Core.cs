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
/// Core command execution logic for iteration optimization
/// </summary>
public partial class OptimizeIterationCommand
{
    public async Task<OptimizeIterationResponse> ExecuteAsync(
        OptimizeIterationRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Optimizing iteration code for platform {Platform}", request.TargetPlatform);
            
            // Analyze the existing code to determine optimization opportunities
            var analysis = await AnalyzeIterationCode(request.ExistingCode, request.TargetPlatform);
            
            // Create iteration context based on analysis
            var context = new IterationContext
            {
                DataSize = analysis.EstimatedDataSize,
                Requirements = ConvertToIterationRequirements(request.Requirements),
                EnvironmentProfile = request.EnvironmentProfile ?? RuntimeEnvironmentDetector.DetectCurrent(),
                PipelineContext = request.PipelineContext,
                TargetPlatform = request.TargetPlatform,
                IsCpuBound = analysis.IsCpuBound,
                IsIoBound = analysis.IsIoBound,
                RequiresAsync = analysis.RequiresAsync
            };
            
            // Select optimal strategy
            var strategy = _strategySelector.SelectStrategy<object>(context);
            
            // Generate optimized code
            var codeGenerationContext = new CodeGenerationContext
            {
                PlatformTarget = request.TargetPlatform,
                CollectionVariableName = analysis.CollectionVariableName,
                ItemVariableName = analysis.ItemVariableName,
                ActionCode = analysis.ActionCode,
                IncludeNullChecks = request.IncludeNullChecks,
                IncludeBoundsChecking = request.IncludeBoundsChecking,
                PerformanceRequirements = request.Requirements,
                AdditionalContext = request.AdditionalContext
            };
            
            var optimizedCode = strategy.GenerateCode(codeGenerationContext);
            
            // Calculate optimization metrics
            var optimizationMetrics = CalculateOptimizationMetrics(analysis, strategy, context);
            
            _logger.LogInformation("Iteration optimization completed. Performance improvement: {Improvement}%", 
                optimizationMetrics.PerformanceImprovementPercentage);
            
            return new OptimizeIterationResponse
            {
                OriginalCode = request.ExistingCode,
                OptimizedCode = optimizedCode,
                SelectedStrategy = strategy,
                OptimizationMetrics = optimizationMetrics,
                Analysis = analysis,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing iteration code");
            return new OptimizeIterationResponse
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
