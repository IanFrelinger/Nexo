using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Feature.Pipeline.Services.Execution.Orchestration
{
    /// <summary>
    /// Handles the orchestration of pipeline execution phases and aggregators
    /// </summary>
    public partial class ExecutionOrchestrator
    {
        private readonly ILogger<PipelineExecutionEngine> _logger;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IResourceOptimizer _resourceOptimizer;
        private readonly ConcurrentDictionary<string, IAggregator> _aggregators = new ConcurrentDictionary<string, IAggregator>();
        private readonly ConcurrentDictionary<string, IBehavior> _behaviors = new ConcurrentDictionary<string, IBehavior>();
        private readonly ConcurrentDictionary<string, ICommand> _commands = new ConcurrentDictionary<string, ICommand>();

        public ExecutionOrchestrator(
            ILogger<PipelineExecutionEngine> logger,
            IResourceMonitor resourceMonitor,
            IResourceOptimizer resourceOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _resourceOptimizer = resourceOptimizer ?? throw new ArgumentNullException(nameof(resourceOptimizer));
        }

        public async Task<PipelineExecutionResult> ExecutePlanAsync(
            IPipelineContext context,
            PipelineExecutionPlan plan,
            CancellationToken cancellationToken)
        {
            var result = new PipelineExecutionResult
            {
                ExecutionId = context.ExecutionId,
                StartTime = DateTime.UtcNow,
                ExecutionPlan = plan
            };

            var aggregatorResults = new List<AggregatorResult>();

            try
            {
                // Monitor initial resource usage
                var initialResourceUsage = await _resourceMonitor.GetResourceUsageAsync(cancellationToken);
                _logger.LogDebug("Initial resource usage - CPU: {CpuUsage}%, Memory: {MemoryUsage}%", 
                    initialResourceUsage.CpuUsagePercentage, initialResourceUsage.Memory.UsagePercentage);

                // Execute phases sequentially
                foreach (var phase in plan.Phases)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogDebug("Executing phase {PhaseNumber}: {PhaseName}", 
                        phase.PhaseNumber, phase.Name);

                    var phaseStartTime = DateTime.UtcNow;
                    var phaseResults = new List<AggregatorResult>();

                    // Check resource usage before phase execution
                    var resourceUsage = await _resourceMonitor.GetResourceUsageAsync(cancellationToken);
                    var throttlingResult = await _resourceOptimizer.CalculateThrottlingAsync(
                        new PipelineExecutionRequest
                        {
                            PipelineId = context.ExecutionId,
                            EstimatedCpuUsage = 10, // Default estimate
                            EstimatedMemoryUsage = 1024 * 1024, // 1MB default
                            EstimatedDuration = TimeSpan.FromMinutes(5), // Default estimate
                            Priority = 1
                        }, 
                        cancellationToken);

                    if (throttlingResult.ShouldThrottle)
                    {
                        _logger.LogWarning("Resource throttling applied - Level: {Level}, Delay: {Delay}ms", 
                            throttlingResult.ThrottlingLevel, throttlingResult.RecommendedDelay.TotalMilliseconds);
                        await Task.Delay(throttlingResult.RecommendedDelay, cancellationToken);
                    }

                    // Execute aggregators in this phase based on strategy
                    switch (phase.ExecutionStrategy)
                    {
                        case AggregatorExecutionStrategy.Sequential:
                            foreach (var aggregatorId in phase.Aggregators)
                            {
                                var aggregatorResult = await ExecuteAggregatorAsync(context, aggregatorId, cancellationToken);
                                phaseResults.Add(aggregatorResult);
                                aggregatorResults.Add(aggregatorResult);
                            }
                            break;

                        case AggregatorExecutionStrategy.Parallel:
                            var parallelTasks = phase.Aggregators.Select(async aggregatorId =>
                            {
                                return await ExecuteAggregatorAsync(context, aggregatorId, cancellationToken);
                            });
                            var parallelResults = await Task.WhenAll(parallelTasks);
                            phaseResults.AddRange(parallelResults);
                            aggregatorResults.AddRange(parallelResults);
                            break;

                        case AggregatorExecutionStrategy.Conditional:
                            foreach (var aggregatorId in phase.Aggregators)
                            {
                                var aggregator = _aggregators[aggregatorId];
                                if (await ShouldExecuteAggregatorAsync(context, aggregator, cancellationToken))
                                {
                                    var aggregatorResult = await ExecuteAggregatorAsync(context, aggregatorId, cancellationToken);
                                    phaseResults.Add(aggregatorResult);
                                    aggregatorResults.Add(aggregatorResult);
                                }
                            }
                            break;

                        default:
                            throw new NotSupportedException($"Execution strategy '{phase.ExecutionStrategy}' is not supported");
                    }

                    var phaseDuration = (DateTime.UtcNow - phaseStartTime).TotalMilliseconds;
                    _logger.LogDebug("Phase {PhaseNumber} completed in {Duration}ms with {ResultCount} results", 
                        phase.PhaseNumber, phaseDuration, phaseResults.Count);

                    // Perform resource optimization after phase completion
                    var optimizationResult = await _resourceOptimizer.OptimizeAsync(cancellationToken);
                    if (optimizationResult.Recommendations.Any())
                    {
                        _logger.LogInformation("Resource optimization recommendations: {Count}", 
                            optimizationResult.Recommendations.Count);
                        foreach (var recommendation in optimizationResult.Recommendations)
                        {
                            _logger.LogDebug("Optimization: {Type} - {Message}", 
                                recommendation.Type, recommendation.Message);
                        }
                    }
                }

                result.IsSuccess = aggregatorResults.All(r => r.IsSuccess);
                result.Status = result.IsSuccess ? ExecutionStatus.Completed : ExecutionStatus.Failed;
                result.AggregatorResults = aggregatorResults;
            }
            catch (OperationCanceledException)
            {
                result.Status = ExecutionStatus.Cancelled;
                result.IsSuccess = false;
            }
            catch (Exception ex)
            {
                result.Status = ExecutionStatus.Failed;
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error during plan execution");
            }

            result.EndTime = DateTime.UtcNow;
            result.ExecutionTimeMs = (long)(result.EndTime - result.StartTime).TotalMilliseconds;

            return result;
        }

        public void RegisterAggregator(IAggregator aggregator)
        {
            if (aggregator == null)
                throw new ArgumentNullException(nameof(aggregator));

            _aggregators[aggregator.Id] = aggregator;
            _logger.LogInformation("Registered aggregator: {AggregatorName} ({AggregatorId})", 
                aggregator.Name, aggregator.Id);
        }

        public void RegisterBehavior(IBehavior behavior)
        {
            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));

            _behaviors[behavior.Id] = behavior;
            _logger.LogInformation("Registered behavior: {BehaviorName} ({BehaviorId})", 
                behavior.Name, behavior.Id);
        }

        public void RegisterCommand(ICommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            _commands[command.Id] = command;
            _logger.LogInformation("Registered command: {CommandName} ({CommandId})", 
                command.Name, command.Id);
        }

        private async Task<AggregatorResult> ExecuteAggregatorAsync(
            IPipelineContext context,
            string aggregatorId,
            CancellationToken cancellationToken)
        {
            var aggregator = _aggregators[aggregatorId];
            var startTime = DateTime.UtcNow;

            _logger.LogDebug("Executing aggregator: {AggregatorName} ({AggregatorId})", 
                aggregator.Name, aggregatorId);

            try
            {
                var result = await aggregator.ExecuteAsync(context);
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogDebug("Aggregator {AggregatorName} completed in {Duration}ms with success: {Success}", 
                    aggregator.Name, duration, result.IsSuccess);

                return result;
            }
            catch (Exception ex)
            {
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                _logger.LogError(ex, "Aggregator {AggregatorName} failed after {Duration}ms", 
                    aggregator.Name, duration);

                return new AggregatorResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    ExecutionTimeMs = (long)duration
                };
            }
        }

        private Task<bool> ShouldExecuteAggregatorAsync(
            IPipelineContext context,
            IAggregator aggregator,
            CancellationToken cancellationToken)
        {
            // Simple conditional logic - can be enhanced with more sophisticated conditions
            foreach (var dependency in aggregator.Dependencies)
            {
                if (dependency.Type == AggregatorDependencyType.Conditional)
                {
                    // Check if the condition is met in the context
                    var conditionValue = context.GetValue<bool>(dependency.DependentAggregatorId, false);
                    if (!conditionValue)
                    {
                        _logger.LogDebug("Skipping aggregator {AggregatorName} due to unmet condition: {Condition}", 
                            aggregator.Name, dependency.DependentAggregatorId);
                        return Task.FromResult(false);
                    }
                }
            }

            return Task.FromResult(true);
        }
    }
}
