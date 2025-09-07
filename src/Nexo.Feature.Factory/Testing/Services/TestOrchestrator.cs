using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Commands;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Services
{
    /// <summary>
    /// Orchestrates the execution of test commands with dependency resolution and parallel execution.
    /// </summary>
    public sealed class TestOrchestrator
    {
        private readonly ILogger<TestOrchestrator> _logger;
        private readonly Dictionary<string, ITestCommand> _commands = new();

        public TestOrchestrator(ILogger<TestOrchestrator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers a test command with the orchestrator.
        /// </summary>
        /// <param name="command">The test command to register</param>
        public void RegisterCommand(ITestCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            
            _commands[command.CommandId] = command;
            _logger.LogInformation("Registered test command: {CommandId}", command.CommandId);
        }

        /// <summary>
        /// Executes all registered test commands with dependency resolution.
        /// </summary>
        /// <param name="context">The test context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test execution results</returns>
        public async Task<TestExecutionSummary> ExecuteAllAsync(ITestContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting test execution for {CommandCount} commands", _commands.Count);

            var results = new Dictionary<string, TestCommandResult>();
            var sharedData = new Dictionary<string, object>();
            var startTime = DateTimeOffset.UtcNow;

            try
            {
                // Build dependency graph
                var dependencyGraph = BuildDependencyGraph();
                var executionOrder = TopologicalSort(dependencyGraph);

                _logger.LogInformation("Test execution order: {ExecutionOrder}", string.Join(" -> ", executionOrder));

                // Calculate total estimated duration for overall timeout
                var totalEstimatedDuration = TimeSpan.FromTicks(executionOrder
                    .Where(id => _commands.ContainsKey(id))
                    .Sum(id => _commands[id].EstimatedDuration.Ticks));
                
                var overallTimeout = TimeSpan.FromMinutes(Math.Max(10, totalEstimatedDuration.TotalMinutes * 2));
                _logger.LogInformation("Overall test execution timeout: {OverallTimeout}", overallTimeout);

                // Create overall timeout cancellation token
                using var overallTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                overallTimeoutCts.CancelAfter(overallTimeout);

                // Execute commands in dependency order
                foreach (var commandId in executionOrder)
                {
                    if (!_commands.TryGetValue(commandId, out var command))
                    {
                        _logger.LogError("Command not found: {CommandId}", commandId);
                        continue;
                    }

                    var commandContext = new TestContextWrapper(context, sharedData);
                    var result = await ExecuteCommandAsync(command, commandContext, overallTimeoutCts.Token);
                    results[commandId] = result;

                    // Add command results to shared data for dependent commands
                    sharedData[commandId] = result;

                    if (!result.ExecutionResult.IsSuccess && command.Priority == TestPriority.Critical)
                    {
                        _logger.LogError("Critical command failed: {CommandId}, stopping execution", commandId);
                        break;
                    }
                }

                var endTime = DateTimeOffset.UtcNow;
                var summary = new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData
                );

                _logger.LogInformation("Test execution completed: {SuccessCount}/{TotalCount} successful", 
                    summary.SuccessfulCommandCount, summary.TotalCommandCount);

                return summary;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                var endTime = DateTimeOffset.UtcNow;
                var totalDuration = endTime - startTime;
                var isTimeout = totalDuration >= TimeSpan.FromMinutes(10);
                var errorMessage = isTimeout 
                    ? $"Overall test execution timed out after {totalDuration.TotalMinutes:F1} minutes"
                    : "Test execution was cancelled";
                
                _logger.LogError(ex, "Test execution failed: {Error}", errorMessage);
                
                return new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData,
                    errorMessage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test execution failed");
                var endTime = DateTimeOffset.UtcNow;
                return new TestExecutionSummary(
                    startTime,
                    endTime,
                    results,
                    sharedData,
                    ex.Message
                );
            }
        }

        /// <summary>
        /// Executes a specific test command.
        /// </summary>
        /// <param name="command">The test command to execute</param>
        /// <param name="context">The test context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test command result</returns>
        public async Task<TestCommandResult> ExecuteCommandAsync(ITestCommand command, ITestContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing test command: {CommandId}", command.CommandId);

            var validationResult = await command.ValidateAsync(context, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogError("Command validation failed: {CommandId}, Errors: {Errors}", 
                    command.CommandId, string.Join(", ", validationResult.Errors));
                
                var failedExecutionResult = new TestExecutionResult(false, TimeSpan.Zero, "Validation failed");
                var failedCleanupResult = new TestCleanupResult(true, TimeSpan.Zero);
                
                return new TestCommandResult(
                    command,
                    validationResult,
                    failedExecutionResult,
                    failedCleanupResult,
                    false
                );
            }

            var executionResult = await command.ExecuteAsync(context, cancellationToken);
            TestCleanupResult? cleanupResult = null;

            if (context.Configuration.CleanupAfterExecution)
            {
                cleanupResult = await command.CleanupAsync(context, executionResult, cancellationToken);
            }

            var isSuccess = executionResult.IsSuccess && (cleanupResult?.IsSuccess ?? true);

            _logger.LogInformation("Command execution completed: {CommandId}, Success: {IsSuccess}, Duration: {Duration}", 
                command.CommandId, isSuccess, executionResult.Duration);

            return new TestCommandResult(
                command,
                validationResult,
                executionResult,
                cleanupResult ?? new TestCleanupResult(true, TimeSpan.Zero),
                isSuccess
            );
        }

        /// <summary>
        /// Gets the status of all registered commands.
        /// </summary>
        /// <returns>Command status information</returns>
        public IReadOnlyList<TestCommandStatus> GetCommandStatuses()
        {
            return _commands.Values.Select(cmd => new TestCommandStatus(
                cmd.CommandId,
                cmd.Name,
                cmd.Category,
                cmd.Priority,
                cmd.EstimatedDuration,
                cmd.CanExecuteInParallel,
                cmd.Dependencies
            )).ToList();
        }

        /// <summary>
        /// Builds a dependency graph for all registered commands.
        /// </summary>
        /// <returns>Dependency graph</returns>
        private Dictionary<string, List<string>> BuildDependencyGraph()
        {
            var graph = new Dictionary<string, List<string>>();

            foreach (var command in _commands.Values)
            {
                graph[command.CommandId] = new List<string>(command.Dependencies);
            }

            return graph;
        }

        /// <summary>
        /// Performs topological sort to determine execution order.
        /// </summary>
        /// <param name="dependencyGraph">The dependency graph</param>
        /// <returns>Execution order</returns>
        private List<string> TopologicalSort(Dictionary<string, List<string>> dependencyGraph)
        {
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();
            var result = new List<string>();

            foreach (var node in dependencyGraph.Keys)
            {
                if (!visited.Contains(node))
                {
                    Visit(node, dependencyGraph, visited, visiting, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Visits a node in the dependency graph for topological sort.
        /// </summary>
        private void Visit(string node, Dictionary<string, List<string>> graph, HashSet<string> visited, HashSet<string> visiting, List<string> result)
        {
            if (visiting.Contains(node))
            {
                throw new InvalidOperationException($"Circular dependency detected involving: {node}");
            }

            if (visited.Contains(node))
            {
                return;
            }

            visiting.Add(node);

            if (graph.TryGetValue(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    Visit(dependency, graph, visited, visiting, result);
                }
            }

            visiting.Remove(node);
            visited.Add(node);
            result.Add(node);
        }
    }

    /// <summary>
    /// Wrapper for test context that includes shared data.
    /// </summary>
    internal sealed class TestContextWrapper : ITestContext
    {
        private readonly ITestContext _baseContext;
        private readonly Dictionary<string, object> _sharedData;

        public TestContextWrapper(ITestContext baseContext, Dictionary<string, object> sharedData)
        {
            _baseContext = baseContext ?? throw new ArgumentNullException(nameof(baseContext));
            _sharedData = sharedData ?? throw new ArgumentNullException(nameof(sharedData));
        }

        public string SessionId => _baseContext.SessionId;
        public TestConfiguration Configuration => _baseContext.Configuration;
        public IDictionary<string, object> SharedData => _sharedData;
        public CancellationToken CancellationToken => _baseContext.CancellationToken;
        public Microsoft.Extensions.Logging.ILogger Logger => _baseContext.Logger;
    }
}
