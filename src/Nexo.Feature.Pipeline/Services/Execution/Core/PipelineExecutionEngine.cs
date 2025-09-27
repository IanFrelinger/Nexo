using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Services.Execution.Planning;
using Nexo.Feature.Pipeline.Services.Execution.Validation;
using Nexo.Feature.Pipeline.Services.Execution.Orchestration;
using Nexo.Feature.Pipeline.Services.Execution.Monitoring;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Feature.Pipeline.Services.Execution.Core
{
    /// <summary>
    /// Core pipeline execution engine that orchestrates the execution of aggregators and behaviors
    /// with support for parallel processing, dependency resolution, and performance monitoring.
    /// </summary>
    public partial class PipelineExecutionEngine : IPipelineExecutionEngine
    {
        private readonly ILogger<PipelineExecutionEngine> _logger;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IResourceOptimizer _resourceOptimizer;
        private readonly ExecutionPlanGenerator _planGenerator;
        private readonly DependencyValidator _dependencyValidator;
        private readonly ExecutionOrchestrator _executionOrchestrator;
        private readonly MetricsCollector _metricsCollector;

        public PipelineExecutionEngine(
            ILogger<PipelineExecutionEngine> logger,
            IResourceMonitor resourceMonitor,
            IResourceOptimizer resourceOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _resourceOptimizer = resourceOptimizer ?? throw new ArgumentNullException(nameof(resourceOptimizer));
            
            // Initialize specialized services
            _planGenerator = new ExecutionPlanGenerator(_logger);
            _dependencyValidator = new DependencyValidator(_logger);
            _executionOrchestrator = new ExecutionOrchestrator(_logger, _resourceMonitor, _resourceOptimizer);
            _metricsCollector = new MetricsCollector(_logger);
        }

        public async Task<PipelineExecutionResult> ExecuteAsync(
            IPipelineContext context,
            List<string> aggregatorIds,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (aggregatorIds == null || !aggregatorIds.Any())
                throw new ArgumentException("Aggregator IDs cannot be null or empty", nameof(aggregatorIds));

            var executionId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;
            
            _logger.LogInformation("Starting pipeline execution {ExecutionId} with {AggregatorCount} aggregators", 
                executionId, aggregatorIds.Count);

            try
            {
                context.Status = PipelineExecutionStatus.Initializing;
                
                // Generate execution plan
                context.Status = PipelineExecutionStatus.Planning;
                var executionPlan = await _planGenerator.GenerateExecutionPlanAsync(context, aggregatorIds, cancellationToken);
                
                // Validate dependencies
                context.Status = PipelineExecutionStatus.Validating;
                var validationResult = await _dependencyValidator.ValidateDependenciesAsync(executionPlan, cancellationToken);
                if (!validationResult.IsValid)
                {
                    return new PipelineExecutionResult
                    {
                        ExecutionId = executionId,
                        IsSuccess = false,
                        Status = ExecutionStatus.Failed,
                        ErrorMessage = $"Dependency validation failed: {validationResult.ErrorMessage}",
                        StartTime = startTime,
                        EndTime = DateTime.UtcNow,
                        ExecutionPlan = executionPlan
                    };
                }

                // Execute the pipeline
                context.Status = PipelineExecutionStatus.Executing;
                var result = await _executionOrchestrator.ExecutePlanAsync(context, executionPlan, cancellationToken);
                
                context.Status = PipelineExecutionStatus.Completed;
                _logger.LogInformation("Pipeline execution {ExecutionId} completed successfully in {Duration}ms", 
                    executionId, result.ExecutionTimeMs);

                return result;
            }
            catch (OperationCanceledException)
            {
                context.Status = PipelineExecutionStatus.Cancelled;
                _logger.LogWarning("Pipeline execution {ExecutionId} was cancelled", executionId);
                
                return new PipelineExecutionResult
                {
                    ExecutionId = executionId,
                    IsSuccess = false,
                    Status = ExecutionStatus.Cancelled,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                context.Status = PipelineExecutionStatus.Failed;
                _logger.LogError(ex, "Pipeline execution {ExecutionId} failed", executionId);
                
                return new PipelineExecutionResult
                {
                    ExecutionId = executionId,
                    IsSuccess = false,
                    Status = ExecutionStatus.Failed,
                    ErrorMessage = ex.Message,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow
                };
            }
        }

        public Task<PipelineExecutionPlan> GenerateExecutionPlanAsync(
            IPipelineContext context,
            List<string> aggregatorIds,
            CancellationToken cancellationToken = default)
        {
            return _planGenerator.GenerateExecutionPlanAsync(context, aggregatorIds, cancellationToken);
        }

        public Task<ExecutionValidationResult> ValidateDependenciesAsync(
            PipelineExecutionPlan plan,
            CancellationToken cancellationToken = default)
        {
            return _dependencyValidator.ValidateDependenciesAsync(plan, cancellationToken);
        }

        public void RegisterAggregator(IAggregator aggregator)
        {
            _executionOrchestrator.RegisterAggregator(aggregator);
        }

        public void RegisterBehavior(IBehavior behavior)
        {
            _executionOrchestrator.RegisterBehavior(behavior);
        }

        public void RegisterCommand(ICommand command)
        {
            _executionOrchestrator.RegisterCommand(command);
        }

        public List<ExecutionMetric> GetExecutionMetrics()
        {
            return _metricsCollector.GetExecutionMetrics();
        }

        public void ClearMetrics()
        {
            _metricsCollector.ClearMetrics();
        }
    }
}
