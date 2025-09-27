using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Enums;

namespace Nexo.Feature.Pipeline.Services.Execution.Validation
{
    /// <summary>
    /// Handles dependency validation for pipeline execution plans
    /// </summary>
    public partial class DependencyValidator
    {
        private readonly ILogger<PipelineExecutionEngine> _logger;
        private readonly ConcurrentDictionary<string, IAggregator> _aggregators = new ConcurrentDictionary<string, IAggregator>();

        public DependencyValidator(ILogger<PipelineExecutionEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<ExecutionValidationResult> ValidateDependenciesAsync(
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
                        return Task.FromResult(validationResult);
                    }
                }
            }

            // Validate circular dependencies
            if (HasCircularDependencies(plan.Dependencies))
            {
                validationResult.IsValid = false;
                validationResult.ErrorMessage = "Circular dependencies detected in execution plan";
                return Task.FromResult(validationResult);
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
                            return Task.FromResult(validationResult);
                        }
                    }
                }
            }

            return Task.FromResult(validationResult);
        }

        public void RegisterAggregator(IAggregator aggregator)
        {
            if (aggregator == null)
                throw new ArgumentNullException(nameof(aggregator));

            _aggregators[aggregator.Id] = aggregator;
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
    }
}
