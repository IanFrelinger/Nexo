using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Dependency and condition evaluation methods for WorkflowExecutionService.
    /// </summary>
    public partial class WorkflowExecutionService
    {
        private List<WorkflowStep> SortStepsByDependencies(List<WorkflowStep> steps)
        {
            var sorted = new List<WorkflowStep>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var step in steps)
            {
                if (!visited.Contains(step.Id))
                {
                    TopologicalSort(step, steps, sorted, visited, visiting);
                }
            }

            return sorted;
        }

        private void TopologicalSort(
            WorkflowStep step,
            List<WorkflowStep> allSteps,
            List<WorkflowStep> sorted,
            HashSet<string> visited,
            HashSet<string> visiting)
        {
            if (visiting.Contains(step.Id))
            {
                throw new InvalidOperationException($"Circular dependency detected for step: {step.Name}");
            }

            if (visited.Contains(step.Id))
            {
                return;
            }

            visiting.Add(step.Id);

            foreach (var dependencyId in step.Dependencies)
            {
                var dependency = allSteps.FirstOrDefault(s => s.Id == dependencyId);
                if (dependency != null)
                {
                    TopologicalSort(dependency, allSteps, sorted, visited, visiting);
                }
            }

            visiting.Remove(step.Id);
            visited.Add(step.Id);
            sorted.Add(step);
        }

        private bool AreDependenciesMet(WorkflowStep step, HashSet<string> completedSteps)
        {
            return step.Dependencies.All(depId => completedSteps.Contains(depId));
        }

        private bool AreConditionsMet(WorkflowStep step, string projectPath)
        {
            foreach (var condition in step.Conditions)
            {
                var isMet = EvaluateCondition(condition, projectPath);
                if (condition.Negate)
                {
                    isMet = !isMet;
                }

                if (!isMet)
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateCondition(StepCondition condition, string projectPath)
        {
            switch (condition.Type)
            {
                case ConditionType.FileExists:
                    var filePath = Path.Combine(projectPath, condition.Value);
                    return File.Exists(filePath);

                case ConditionType.EnvironmentVariable:
                    var envValue = Environment.GetEnvironmentVariable(condition.Value);
                    return !string.IsNullOrEmpty(envValue);

                case ConditionType.PreviousStepResult:
                    // This would need to be evaluated in context of previous steps
                    return true;

                case ConditionType.Custom:
                    // Custom condition evaluation would be implemented here
                    return true;

                default:
                    return true;
            }
        }
    }
}
