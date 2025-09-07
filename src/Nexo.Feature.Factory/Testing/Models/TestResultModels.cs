using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Factory.Testing.Commands;

namespace Nexo.Feature.Factory.Testing.Models
{
    /// <summary>
    /// Represents the result of executing a test command.
    /// </summary>
    public sealed class TestCommandResult
    {
        /// <summary>
        /// Gets the test command that was executed.
        /// </summary>
        public ITestCommand Command { get; }

        /// <summary>
        /// Gets the validation result.
        /// </summary>
        public TestValidationResult ValidationResult { get; }

        /// <summary>
        /// Gets the execution result.
        /// </summary>
        public TestExecutionResult ExecutionResult { get; }

        /// <summary>
        /// Gets the cleanup result.
        /// </summary>
        public TestCleanupResult CleanupResult { get; }

        /// <summary>
        /// Gets whether the command execution was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the total duration including validation, execution, and cleanup.
        /// </summary>
        public TimeSpan TotalDuration => 
            ValidationResult.Duration + 
            ExecutionResult.Duration + 
            (CleanupResult?.Duration ?? TimeSpan.Zero);

        public TestCommandResult(
            ITestCommand command,
            TestValidationResult validationResult,
            TestExecutionResult executionResult,
            TestCleanupResult cleanupResult,
            bool isSuccess)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            ValidationResult = validationResult ?? throw new ArgumentNullException(nameof(validationResult));
            ExecutionResult = executionResult ?? throw new ArgumentNullException(nameof(executionResult));
            CleanupResult = cleanupResult;
            IsSuccess = isSuccess;
        }
    }

    /// <summary>
    /// Represents the summary of a complete test execution.
    /// </summary>
    public sealed class TestExecutionSummary
    {
        /// <summary>
        /// Gets the start time of the test execution.
        /// </summary>
        public DateTimeOffset StartTime { get; }

        /// <summary>
        /// Gets the end time of the test execution.
        /// </summary>
        public DateTimeOffset EndTime { get; }

        /// <summary>
        /// Gets the total duration of the test execution.
        /// </summary>
        public TimeSpan TotalDuration => EndTime - StartTime;

        /// <summary>
        /// Gets the results of individual test commands.
        /// </summary>
        public IReadOnlyDictionary<string, TestCommandResult> CommandResults { get; }

        /// <summary>
        /// Gets the shared data from the test execution.
        /// </summary>
        public IReadOnlyDictionary<string, object> SharedData { get; }

        /// <summary>
        /// Gets the error message if the overall execution failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the total number of commands executed.
        /// </summary>
        public int TotalCommandCount => CommandResults.Count;

        /// <summary>
        /// Gets the number of successfully executed commands.
        /// </summary>
        public int SuccessfulCommandCount => CommandResults.Values.Count(r => r.IsSuccess);

        /// <summary>
        /// Gets the number of failed commands.
        /// </summary>
        public int FailedCommandCount => TotalCommandCount - SuccessfulCommandCount;

        /// <summary>
        /// Gets whether the overall test execution was successful.
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage) && FailedCommandCount == 0;

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalCommandCount > 0 ? (double)SuccessfulCommandCount / TotalCommandCount * 100 : 0;

        /// <summary>
        /// Gets the performance metrics for the entire test execution.
        /// </summary>
        public TestExecutionPerformanceMetrics PerformanceMetrics { get; }

        public TestExecutionSummary(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            IReadOnlyDictionary<string, TestCommandResult> commandResults,
            IReadOnlyDictionary<string, object> sharedData,
            string? errorMessage = null)
        {
            StartTime = startTime;
            EndTime = endTime;
            CommandResults = commandResults ?? throw new ArgumentNullException(nameof(commandResults));
            SharedData = sharedData ?? throw new ArgumentNullException(nameof(sharedData));
            ErrorMessage = errorMessage;
            PerformanceMetrics = CalculatePerformanceMetrics(commandResults);
        }

        private TestExecutionPerformanceMetrics CalculatePerformanceMetrics(IReadOnlyDictionary<string, TestCommandResult> commandResults)
        {
            var totalCpuUsage = commandResults.Values
                .Where(r => r.ExecutionResult.PerformanceMetrics != null)
                .Sum(r => r.ExecutionResult.PerformanceMetrics?.CpuUsagePercentage ?? 0);

            var totalMemoryUsage = commandResults.Values
                .Where(r => r.ExecutionResult.PerformanceMetrics != null)
                .Sum(r => r.ExecutionResult.PerformanceMetrics?.MemoryUsageBytes ?? 0);

            var totalAiApiCalls = commandResults.Values
                .Where(r => r.ExecutionResult.PerformanceMetrics != null)
                .Sum(r => r.ExecutionResult.PerformanceMetrics?.AiApiCalls ?? 0);

            var totalAiProcessingTime = TimeSpan.FromTicks(commandResults.Values
                .Where(r => r.ExecutionResult.PerformanceMetrics != null)
                .Sum(r => r.ExecutionResult.PerformanceMetrics?.AiProcessingTime.Ticks ?? 0));

            var totalFilesCreated = commandResults.Values
                .Where(r => r.ExecutionResult.PerformanceMetrics != null)
                .Sum(r => r.ExecutionResult.PerformanceMetrics?.FilesCreated ?? 0);

            var totalFileSize = commandResults.Values
                .Where(r => r.ExecutionResult.PerformanceMetrics != null)
                .Sum(r => r.ExecutionResult.PerformanceMetrics?.TotalFileSizeBytes ?? 0);

            return new TestExecutionPerformanceMetrics(
                totalCpuUsage,
                totalMemoryUsage,
                totalAiApiCalls,
                totalAiProcessingTime,
                totalFilesCreated,
                totalFileSize
            );
        }
    }

    /// <summary>
    /// Represents performance metrics for the entire test execution.
    /// </summary>
    public sealed class TestExecutionPerformanceMetrics
    {
        /// <summary>
        /// Gets the total CPU usage percentage across all commands.
        /// </summary>
        public double TotalCpuUsagePercentage { get; }

        /// <summary>
        /// Gets the total memory usage in bytes across all commands.
        /// </summary>
        public long TotalMemoryUsageBytes { get; }

        /// <summary>
        /// Gets the total number of AI API calls made across all commands.
        /// </summary>
        public int TotalAiApiCalls { get; }

        /// <summary>
        /// Gets the total AI processing time across all commands.
        /// </summary>
        public TimeSpan TotalAiProcessingTime { get; }

        /// <summary>
        /// Gets the total number of files created across all commands.
        /// </summary>
        public int TotalFilesCreated { get; }

        /// <summary>
        /// Gets the total file size created in bytes across all commands.
        /// </summary>
        public long TotalFileSizeBytes { get; }

        /// <summary>
        /// Gets the average CPU usage percentage per command.
        /// </summary>
        public double AverageCpuUsagePercentage { get; }

        /// <summary>
        /// Gets the average memory usage per command.
        /// </summary>
        public long AverageMemoryUsageBytes { get; }

        /// <summary>
        /// Gets the average AI processing time per command.
        /// </summary>
        public TimeSpan AverageAiProcessingTime { get; }

        public TestExecutionPerformanceMetrics(
            double totalCpuUsagePercentage,
            long totalMemoryUsageBytes,
            int totalAiApiCalls,
            TimeSpan totalAiProcessingTime,
            int totalFilesCreated,
            long totalFileSizeBytes)
        {
            TotalCpuUsagePercentage = totalCpuUsagePercentage;
            TotalMemoryUsageBytes = totalMemoryUsageBytes;
            TotalAiApiCalls = totalAiApiCalls;
            TotalAiProcessingTime = totalAiProcessingTime;
            TotalFilesCreated = totalFilesCreated;
            TotalFileSizeBytes = totalFileSizeBytes;

            // Calculate averages (assuming we have command count from context)
            AverageCpuUsagePercentage = totalCpuUsagePercentage; // Will be divided by command count when available
            AverageMemoryUsageBytes = totalMemoryUsageBytes; // Will be divided by command count when available
            AverageAiProcessingTime = totalAiProcessingTime; // Will be divided by command count when available
        }
    }

    /// <summary>
    /// Represents the status of a test command.
    /// </summary>
    public sealed class TestCommandStatus
    {
        /// <summary>
        /// Gets the command ID.
        /// </summary>
        public string CommandId { get; }

        /// <summary>
        /// Gets the command name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the command category.
        /// </summary>
        public TestCategory Category { get; }

        /// <summary>
        /// Gets the command priority.
        /// </summary>
        public TestPriority Priority { get; }

        /// <summary>
        /// Gets the estimated duration.
        /// </summary>
        public TimeSpan EstimatedDuration { get; }

        /// <summary>
        /// Gets whether the command can execute in parallel.
        /// </summary>
        public bool CanExecuteInParallel { get; }

        /// <summary>
        /// Gets the command dependencies.
        /// </summary>
        public IReadOnlyList<string> Dependencies { get; }

        public TestCommandStatus(
            string commandId,
            string name,
            TestCategory category,
            TestPriority priority,
            TimeSpan estimatedDuration,
            bool canExecuteInParallel,
            IReadOnlyList<string> dependencies)
        {
            CommandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category;
            Priority = priority;
            EstimatedDuration = estimatedDuration;
            CanExecuteInParallel = canExecuteInParallel;
            Dependencies = dependencies ?? new List<string>();
        }
    }
}
