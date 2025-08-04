using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Shared.Interfaces.Resource;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Core pipeline execution engine that orchestrates the execution of aggregators and behaviors
    /// with support for parallel processing, dependency resolution, and performance monitoring.
    /// </summary>
    public class PipelineExecutionEngine : IPipelineExecutionEngine
    {
        private readonly ILogger<PipelineExecutionEngine> _logger;
        private readonly IResourceMonitor _resourceMonitor;
        private readonly IResourceOptimizer _resourceOptimizer;
        private readonly ConcurrentDictionary<string, IAggregator> _aggregators = new ConcurrentDictionary<string, IAggregator>();
        private readonly ConcurrentDictionary<string, IBehavior> _behaviors = new ConcurrentDictionary<string, IBehavior>();
        private readonly ConcurrentDictionary<string, ICommand> _commands = new ConcurrentDictionary<string, ICommand>();
        private readonly List<ExecutionMetric> _executionMetrics = new List<ExecutionMetric>();
        private readonly object _metricsLock = new object();

        public PipelineExecutionEngine(
            ILogger<PipelineExecutionEngine> logger,
            IResourceMonitor resourceMonitor,
            IResourceOptimizer resourceOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resourceMonitor = resourceMonitor ?? throw new ArgumentNullException(nameof(resourceMonitor));
            _resourceOptimizer = resourceOptimizer ?? throw new ArgumentNullException(nameof(resourceOptimizer));
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
                var executionPlan = await GenerateExecutionPlanAsync(context, aggregatorIds, cancellationToken);
                
                // Validate dependencies
                context.Status = PipelineExecutionStatus.Validating;
                var validationResult = await ValidateDependenciesAsync(executionPlan, cancellationToken);
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
                var result = await ExecutePlanAsync(context, executionPlan, cancellationToken);
                
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

        public async Task<PipelineExecutionPlan> GenerateExecutionPlanAsync(
            IPipelineContext context,
            List<string> aggregatorIds,
            CancellationToken cancellationToken = default)
        {
            var plan = new PipelineExecutionPlan
            {
                ExecutionId = context.ExecutionId,
                GeneratedAt = DateTime.UtcNow
            };

            var phases = new List<PipelineExecutionPhase>();
            var dependencies = new List<ExecutionDependency>();
            var totalEstimatedTime = 0L;

            // Group aggregators by execution strategy
            var aggregatorGroups = new Dictionary<AggregatorExecutionStrategy, List<IAggregator>>();
            
            foreach (var aggregatorId in aggregatorIds)
            {
                if (!_aggregators.TryGetValue(aggregatorId, out var aggregator))
                {
                    throw new InvalidOperationException($"Aggregator '{aggregatorId}' not found");
                }

                if (!aggregatorGroups.ContainsKey(aggregator.ExecutionStrategy))
                {
                    aggregatorGroups[aggregator.ExecutionStrategy] = new List<IAggregator>();
                }
                aggregatorGroups[aggregator.ExecutionStrategy].Add(aggregator);
            }

            // Create phases based on execution strategy
            var phaseNumber = 1;
            foreach (var group in aggregatorGroups)
            {
                var phase = new PipelineExecutionPhase
                {
                    PhaseNumber = phaseNumber++,
                    Name = $"Phase {phaseNumber - 1}: {group.Key}",
                    ExecutionStrategy = group.Key,
                    Aggregators = group.Value.Select(a => a.Id).ToList(),
                    EstimatedExecutionTimeMs = group.Value.Sum(a => EstimateExecutionTime(a))
                };

                phases.Add(phase);
                totalEstimatedTime += phase.EstimatedExecutionTimeMs;

                // Add dependencies between phases
                if (phases.Count > 1)
                {
                    dependencies.Add(new ExecutionDependency
                    {
                        DependentId = phases[phases.Count - 2].PhaseNumber.ToString(),
                        DependencyId = phase.PhaseNumber.ToString(),
                        DependencyType = Nexo.Feature.Pipeline.Enums.DependencyType.PhaseOrder
                    });
                }
            }

            plan.Phases = phases;
            plan.Dependencies = dependencies;
            plan.EstimatedExecutionTimeMs = totalEstimatedTime;

            _logger.LogDebug("Generated execution plan with {PhaseCount} phases, estimated time: {EstimatedTime}ms", 
                phases.Count, totalEstimatedTime);

            return plan;
        }

        public async Task<ExecutionValidationResult> ValidateDependenciesAsync(
            PipelineExecutionPlan plan,
            CancellationToken cancellationToken = default)
        {
            var validationResult = new ExecutionValidationResult { IsValid = true };

            // Validate that all referenced aggregators exist
            foreach (var phase in plan.Phases)
            {
                foreach (var aggregatorId in phase.Aggregators)
                {
                    if (!_aggregators.ContainsKey(aggregatorId))
                    {
                        validationResult.IsValid = false;
                        validationResult.ErrorMessage = $"Aggregator '{aggregatorId}' not found";
                        return validationResult;
                    }
                }
            }

            // Validate circular dependencies
            if (HasCircularDependencies(plan.Dependencies))
            {
                validationResult.IsValid = false;
                validationResult.ErrorMessage = "Circular dependencies detected in execution plan";
                return validationResult;
            }

            // Validate aggregator dependencies
            foreach (var phase in plan.Phases)
            {
                foreach (var aggregatorId in phase.Aggregators)
                {
                    var aggregator = _aggregators[aggregatorId];
                    foreach (var dependency in aggregator.Dependencies)
                    {
                        if (!IsDependencySatisfied(dependency, plan))
                        {
                            validationResult.IsValid = false;
                            validationResult.ErrorMessage = $"Dependency '{dependency.DependentAggregatorId}' for aggregator '{aggregatorId}' is not satisfied";
                            return validationResult;
                        }
                    }
                }
            }

            return validationResult;
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

        public List<ExecutionMetric> GetExecutionMetrics()
        {
            lock (_metricsLock)
            {
                return _executionMetrics.ToList();
            }
        }

        public void ClearMetrics()
        {
            lock (_metricsLock)
            {
                _executionMetrics.Clear();
            }
        }

        private async Task<PipelineExecutionResult> ExecutePlanAsync(
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

                    // Record metrics
                    RecordExecutionMetric("Phase", phase.Name, phaseDuration, phaseResults.Count);

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

                RecordExecutionMetric("Aggregator", aggregator.Name, duration, result.IsSuccess ? 1 : 0);
                
                _logger.LogDebug("Aggregator {AggregatorName} completed in {Duration}ms with success: {Success}", 
                    aggregator.Name, duration, result.IsSuccess);

                return result;
            }
            catch (Exception ex)
            {
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                RecordExecutionMetric("Aggregator", aggregator.Name, duration, 0);
                
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

        private async Task<bool> ShouldExecuteAggregatorAsync(
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
                        return false;
                    }
                }
            }

            return true;
        }

        private long EstimateExecutionTime(IAggregator aggregator)
        {
            // Simple estimation - can be enhanced with historical data
            var baseTime = 5000L; // 5 seconds base time
            var behaviorCount = aggregator.Behaviors.Count;
            var commandCount = aggregator.DirectCommands.Count;
            
            return baseTime + (behaviorCount * 2000) + (commandCount * 1000);
        }

        private bool HasCircularDependencies(List<ExecutionDependency> dependencies)
        {
            // Simple cycle detection using DFS
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var dependency in dependencies)
            {
                if (!visited.Contains(dependency.DependencyId))
                {
                    if (HasCycle(dependency.DependencyId, dependencies, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasCycle(string phaseId, List<ExecutionDependency> dependencies, 
            HashSet<string> visited, HashSet<string> recursionStack)
        {
            visited.Add(phaseId);
            recursionStack.Add(phaseId);

            var outgoingDependencies = dependencies.Where(d => d.DependentId == phaseId);
            foreach (var dependency in outgoingDependencies)
            {
                if (!visited.Contains(dependency.DependencyId))
                {
                    if (HasCycle(dependency.DependencyId, dependencies, visited, recursionStack))
                    {
                        return true;
                    }
                }
                else if (recursionStack.Contains(dependency.DependencyId))
                {
                    return true;
                }
            }

            recursionStack.Remove(phaseId);
            return false;
        }

        private bool IsDependencySatisfied(AggregatorDependency dependency, PipelineExecutionPlan plan)
        {
            // Check if the dependency is satisfied by any phase in the plan
            foreach (var phase in plan.Phases)
            {
                if (phase.Aggregators.Contains(dependency.DependentAggregatorId))
                {
                    return true;
                }
            }

            return false;
        }

        private void RecordExecutionMetric(string category, string name, double durationMs, int count)
        {
            lock (_metricsLock)
            {
                _executionMetrics.Add(new ExecutionMetric
                {
                    Category = category,
                    Name = name,
                    DurationMs = durationMs,
                    Count = count,
                    Timestamp = DateTime.UtcNow
                });

                // Keep only the last 1000 metrics
                if (_executionMetrics.Count > 1000)
                {
                    _executionMetrics.RemoveRange(0, _executionMetrics.Count - 1000);
                }
            }
        }
    }
} 