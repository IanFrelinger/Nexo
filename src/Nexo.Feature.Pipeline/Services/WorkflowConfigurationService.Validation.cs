using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Validation functionality for workflow configuration service.
    /// </summary>
    public partial class WorkflowConfigurationService
    {
        public Task<WorkflowValidationResult> ValidateAsync(WorkflowConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var result = new WorkflowValidationResult { IsValid = true };

            // Validate basic properties
            if (string.IsNullOrEmpty(configuration.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Configuration name is required");
            }

            if (configuration.Steps == null || !configuration.Steps.Any())
            {
                result.IsValid = false;
                result.Errors.Add("Configuration must have at least one step");
            }

            // Validate steps
            if (configuration.Steps != null)
            {
                var stepIds = new HashSet<string>();
                foreach (var step in configuration.Steps)
                {
                    // Check for duplicate step IDs
                    if (!stepIds.Add(step.Id))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Duplicate step ID found: {step.Id}");
                    }

                    // Validate step properties
                    if (string.IsNullOrEmpty(step.Name))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Step {step.Id} must have a name");
                    }

                    if (string.IsNullOrEmpty(step.Command) && step.Type != StepType.Custom)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Step {step.Name} must have a command");
                    }

                    // Validate dependencies
                    foreach (var dependencyId in step.Dependencies)
                    {
                        if (!configuration.Steps.Any(s => s.Id == dependencyId))
                        {
                            result.IsValid = false;
                            result.Errors.Add($"Step {step.Name} depends on non-existent step: {dependencyId}");
                        }
                    }
                }

                // Check for circular dependencies
                if (HasCircularDependencies(configuration.Steps))
                {
                    result.IsValid = false;
                    result.Errors.Add("Circular dependencies detected in workflow steps");
                }
            }

            return Task.FromResult(result);
        }

        private bool HasCircularDependencies(List<WorkflowStep> steps)
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var step in steps)
            {
                if (!visited.Contains(step.Id))
                {
                    if (HasCycle(step.Id, steps, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool HasCycle(string stepId, List<WorkflowStep> steps, HashSet<string> visited, HashSet<string> recursionStack)
        {
            if (recursionStack.Contains(stepId))
            {
                return true;
            }

            if (visited.Contains(stepId))
            {
                return false;
            }

            visited.Add(stepId);
            recursionStack.Add(stepId);

            var step = steps.FirstOrDefault(s => s.Id == stepId);
            if (step != null)
            {
                foreach (var dependencyId in step.Dependencies)
                {
                    if (HasCycle(dependencyId, steps, visited, recursionStack))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(stepId);
            return false;
        }
    }
}
