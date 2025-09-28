using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Application.Commands.Planning
{
    /// <summary>
    /// Engine responsible for creating execution plans for commands
    /// </summary>
    /// <typeparam name="TInput">The input type for commands</typeparam>
    /// <typeparam name="TOutput">The output type for commands</typeparam>
    public class CommandPlanningEngine<TInput, TOutput>
    {
        /// <summary>
        /// Creates an execution plan for the given commands
        /// </summary>
        public CommandExecutionPlan GetExecutionPlan(IEnumerable<ICommand<TInput, TOutput>> commands)
        {
            var commandList = commands.ToList();
            var executionOrder = new List<string>();

            // Simple execution order - commands in the order they were provided
            // In a more complex scenario, this could analyze dependencies
            executionOrder.AddRange(commandList.Select(c => c.CommandId));

            return new CommandExecutionPlan
            {
                Commands = commandList.Cast<object>().ToList(),
                TypedCommands = commandList.Cast<ICommand<object, object>>().ToList(),
                ExecutionOrder = executionOrder,
                EstimatedDuration = EstimateExecutionDuration(commandList),
                Dependencies = AnalyzeDependencies(commandList)
            };
        }

        /// <summary>
        /// Creates an optimized execution plan based on dependencies
        /// </summary>
        public CommandExecutionPlan GetOptimizedExecutionPlan(IEnumerable<ICommand<TInput, TOutput>> commands)
        {
            var commandList = commands.ToList();
            var executionOrder = new List<string>();
            var dependencies = AnalyzeDependencies(commandList);

            // Topological sort to determine execution order
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var command in commandList)
            {
                if (!visited.Contains(command.CommandId))
                {
                    if (!TopologicalSort(command.CommandId, dependencies, visited, visiting, executionOrder))
                    {
                        // Circular dependency detected
                        throw new InvalidOperationException("Circular dependency detected in command execution plan");
                    }
                }
            }

            return new CommandExecutionPlan
            {
                Commands = commandList.Cast<object>().ToList(),
                TypedCommands = commandList.Cast<ICommand<object, object>>().ToList(),
                ExecutionOrder = executionOrder,
                EstimatedDuration = EstimateExecutionDuration(commandList),
                Dependencies = dependencies
            };
        }

        /// <summary>
        /// Estimates the total execution duration for the commands
        /// </summary>
        private TimeSpan EstimateExecutionDuration(List<ICommand<TInput, TOutput>> commands)
        {
            // Simple estimation - in a real scenario, this could be based on historical data
            var baseDuration = TimeSpan.FromSeconds(1);
            return TimeSpan.FromTicks(baseDuration.Ticks * commands.Count);
        }

        /// <summary>
        /// Analyzes dependencies between commands
        /// </summary>
        private Dictionary<string, List<string>> AnalyzeDependencies(List<ICommand<TInput, TOutput>> commands)
        {
            var dependencies = new Dictionary<string, List<string>>();

            foreach (var command in commands)
            {
                dependencies[command.CommandId] = new List<string>();
                // In a real scenario, this would analyze actual dependencies
                // For now, we'll assume no dependencies
            }

            return dependencies;
        }

        /// <summary>
        /// Performs topological sort to determine execution order
        /// </summary>
        private bool TopologicalSort(string commandId, Dictionary<string, List<string>> dependencies, 
            HashSet<string> visited, HashSet<string> visiting, List<string> result)
        {
            if (visiting.Contains(commandId))
            {
                return false; // Circular dependency
            }

            if (visited.Contains(commandId))
            {
                return true; // Already processed
            }

            visiting.Add(commandId);

            foreach (var dependency in dependencies.GetValueOrDefault(commandId, new List<string>()))
            {
                if (!TopologicalSort(dependency, dependencies, visited, visiting, result))
                {
                    return false;
                }
            }

            visiting.Remove(commandId);
            visited.Add(commandId);
            result.Add(commandId);

            return true;
        }
    }
}
