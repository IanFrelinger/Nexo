using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Commands
{
    /// <summary>
    /// Simple test command implementation for basic testing scenarios.
    /// </summary>
    public sealed class SimpleTestCommand : ITestCommand
    {
        private readonly ILogger? _logger;

        public string CommandId { get; }
        public string Name { get; }
        public string Description { get; }
        public TestCategory Category { get; }
        public TestPriority Priority { get; }
        public TimeSpan EstimatedDuration { get; }
        public bool CanExecuteInParallel { get; }
        public string[] Dependencies { get; }

        public SimpleTestCommand(
            string commandId,
            string name,
            string description,
            TestCategory category,
            TestPriority priority,
            TimeSpan estimatedDuration,
            bool canExecuteInParallel = true,
            string[]? dependencies = null,
            ILogger? logger = null)
        {
            CommandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Category = category;
            Priority = priority;
            EstimatedDuration = estimatedDuration;
            CanExecuteInParallel = canExecuteInParallel;
            Dependencies = dependencies ?? new string[0];
            _logger = logger;
        }

        public Task<TestValidationResult> ValidateAsync(ITestContext context, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Validating simple test command: {CommandId}", CommandId);
            
            var errors = new List<string>();
            var warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(CommandId))
                errors.Add("Command ID cannot be null or empty");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Command name cannot be null or empty");

            warnings.Add($"Simple test command {CommandId} validation completed");

            return Task.FromResult(new TestValidationResult(errors.Count == 0, errors, warnings, TimeSpan.Zero));
        }

        public Task<TestExecutionResult> ExecuteAsync(ITestContext context, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Executing simple test command: {CommandId}", CommandId);
            
            // Simple mock execution
            var startTime = DateTimeOffset.UtcNow;
            
            // Simulate some work
            Thread.Sleep(100);
            
            var duration = DateTimeOffset.UtcNow - startTime;
            var outputData = new Dictionary<string, object>
            {
                ["commandId"] = CommandId,
                ["executedAt"] = startTime,
                ["duration"] = duration
            };

            var performanceMetrics = new TestPerformanceMetrics(
                cpuUsagePercentage: 0.1,
                memoryUsageBytes: 1024,
                aiApiCalls: 0,
                aiProcessingTime: TimeSpan.Zero,
                filesCreated: 0,
                totalFileSizeBytes: 0
            );

            var result = new TestExecutionResult(
                isSuccess: true,
                duration: duration,
                errorMessage: null,
                outputData: outputData,
                performanceMetrics: performanceMetrics,
                artifacts: new List<TestArtifact>()
            );

            return Task.FromResult(result);
        }

        public Task<TestCleanupResult> CleanupAsync(ITestContext context, TestExecutionResult result, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Cleaning up simple test command: {CommandId}", CommandId);
            
            // Simple mock cleanup
            var startTime = DateTimeOffset.UtcNow;
            Thread.Sleep(10);
            var duration = DateTimeOffset.UtcNow - startTime;

            return Task.FromResult(new TestCleanupResult(true, duration, null, 0));
        }
    }
}
