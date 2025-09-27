using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Services.Execution.Planning
{
    /// <summary>
    /// Handles execution plan generation for pipeline aggregators
    /// </summary>
    public class ExecutionPlanGenerator
    {
        private readonly ILogger<PipelineExecutionEngine> _logger;
        private readonly ConcurrentDictionary<string, IAggregator> _aggregators = new ConcurrentDictionary<string, IAggregator>();

        public ExecutionPlanGenerator(ILogger<PipelineExecutionEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<PipelineExecutionPlan> GenerateExecutionPlanAsync(
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

            return Task.FromResult(plan);
        }

        public void RegisterAggregator(IAggregator aggregator)
        {
            if (aggregator == null)
                throw new ArgumentNullException(nameof(aggregator));

            _aggregators[aggregator.Id] = aggregator;
            _logger.LogInformation("Registered aggregator: {AggregatorName} ({AggregatorId})", 
                aggregator.Name, aggregator.Id);
        }

        private long EstimateExecutionTime(IAggregator aggregator)
        {
            // Simple estimation - can be enhanced with historical data
            var baseTime = 5000L; // 5 seconds base time
            var behaviorCount = aggregator.Behaviors.Count;
            var commandCount = aggregator.DirectCommands.Count;
            
            return baseTime + (behaviorCount * 2000) + (commandCount * 1000);
        }
    }
}
