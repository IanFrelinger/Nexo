using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Models;

namespace Nexo.Feature.Factory.Testing.Commands
{
    /// <summary>
    /// Base class for test commands providing common functionality.
    /// </summary>
    public abstract class TestCommandBase : ITestCommand
    {
        protected readonly ILogger _logger;

        protected TestCommandBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public abstract string CommandId { get; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract TestCategory Category { get; }
        public abstract TestPriority Priority { get; }
        public abstract TimeSpan EstimatedDuration { get; }
        public abstract bool CanExecuteInParallel { get; }
        public abstract string[] Dependencies { get; }

        public virtual async Task<TestValidationResult> ValidateAsync(ITestContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                _logger.LogInformation("Validating test command: {CommandId}", CommandId);

                // Basic validation
                if (string.IsNullOrWhiteSpace(CommandId))
                    errors.Add("Command ID cannot be null or empty");

                if (string.IsNullOrWhiteSpace(Name))
                    errors.Add("Command name cannot be null or empty");

                if (string.IsNullOrWhiteSpace(Description))
                    errors.Add("Command description cannot be null or empty");

                // Validate dependencies
                foreach (var dependency in Dependencies)
                {
                    if (!context.SharedData.ContainsKey(dependency))
                    {
                        errors.Add($"Dependency '{dependency}' not found in shared data");
                    }
                }

                // Validate output directory
                if (!string.IsNullOrEmpty(context.Configuration.OutputDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(context.Configuration.OutputDirectory);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Cannot create output directory: {ex.Message}");
                    }
                }

                // Custom validation
                await ValidateInternalAsync(context, errors, warnings, cancellationToken);

                stopwatch.Stop();
                var isValid = errors.Count == 0;

                _logger.LogInformation("Validation completed for {CommandId}: {IsValid} ({Duration}ms)", 
                    CommandId, isValid, stopwatch.ElapsedMilliseconds);

                return new TestValidationResult(isValid, errors, warnings, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Validation failed for command: {CommandId}", CommandId);
                errors.Add($"Validation error: {ex.Message}");
                return new TestValidationResult(false, errors, warnings, stopwatch.Elapsed);
            }
        }

        public virtual async Task<TestExecutionResult> ExecuteAsync(ITestContext context, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var artifacts = new List<TestArtifact>();
            var outputData = new Dictionary<string, object>();
            var performanceMetrics = new TestPerformanceMetrics(0, 0, 0, TimeSpan.Zero, 0, 0);

            try
            {
                var commandTimeout = context.Configuration.GetTimeoutForCommand(CommandId);
                _logger.LogInformation("Executing test command: {CommandId} with timeout: {Timeout}", 
                    CommandId, commandTimeout);

                // Create a timeout cancellation token
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(commandTimeout);

                // Execute the test with timeout
                var result = await ExecuteInternalAsync(context, outputData, artifacts, timeoutCts.Token);

                stopwatch.Stop();

                // Update performance metrics
                performanceMetrics = new TestPerformanceMetrics(
                    GetCpuUsage(),
                    GetMemoryUsage(),
                    GetAiApiCalls(outputData),
                    GetAiProcessingTime(outputData),
                    artifacts.Count,
                    GetTotalFileSize(artifacts)
                );

                _logger.LogInformation("Test command {CommandId} completed: {IsSuccess} ({Duration}ms)", 
                    CommandId, result, stopwatch.ElapsedMilliseconds);

                return new TestExecutionResult(
                    result,
                    stopwatch.Elapsed,
                    null,
                    outputData,
                    performanceMetrics,
                    artifacts
                );
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                var commandTimeout = context.Configuration.GetTimeoutForCommand(CommandId);
                var isTimeout = stopwatch.Elapsed >= commandTimeout;
                var errorMessage = isTimeout 
                    ? $"Test command {CommandId} timed out after {commandTimeout.TotalSeconds:F1} seconds"
                    : "Test command was cancelled";
                
                _logger.LogError(ex, "Test command {CommandId} failed: {Error}", CommandId, errorMessage);
                
                outputData["TimeoutOccurred"] = isTimeout;
                outputData["ActualDuration"] = stopwatch.Elapsed;
                outputData["TimeoutDuration"] = commandTimeout;
                
                return new TestExecutionResult(
                    false,
                    stopwatch.Elapsed,
                    errorMessage,
                    outputData,
                    performanceMetrics,
                    artifacts
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Test command {CommandId} failed", CommandId);
                return new TestExecutionResult(
                    false,
                    stopwatch.Elapsed,
                    ex.Message,
                    outputData,
                    performanceMetrics,
                    artifacts
                );
            }
        }

        public virtual async Task<TestCleanupResult> CleanupAsync(ITestContext context, TestExecutionResult result, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var artifactsCleanedUp = 0;

            try
            {
                _logger.LogInformation("Cleaning up test command: {CommandId} with timeout: {Timeout}", 
                    CommandId, context.Configuration.CleanupTimeout);

                // Create a timeout cancellation token for cleanup
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(context.Configuration.CleanupTimeout);

                if (context.Configuration.CleanupAfterExecution)
                {
                    foreach (var artifact in result.Artifacts)
                    {
                        if (artifact.ShouldCleanup && File.Exists(artifact.FilePath))
                        {
                            try
                            {
                                File.Delete(artifact.FilePath);
                                artifactsCleanedUp++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete artifact: {FilePath}", artifact.FilePath);
                            }
                        }
                    }
                }

                // Custom cleanup with timeout
                await CleanupInternalAsync(context, result, timeoutCts.Token);

                stopwatch.Stop();

                _logger.LogInformation("Cleanup completed for {CommandId}: {ArtifactsCleanedUp} artifacts ({Duration}ms)", 
                    CommandId, artifactsCleanedUp, stopwatch.ElapsedMilliseconds);

                return new TestCleanupResult(true, stopwatch.Elapsed, null, artifactsCleanedUp);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                var isTimeout = stopwatch.Elapsed >= context.Configuration.CleanupTimeout;
                var errorMessage = isTimeout 
                    ? $"Cleanup for command {CommandId} timed out after {context.Configuration.CleanupTimeout.TotalSeconds:F1} seconds"
                    : "Cleanup was cancelled";
                
                _logger.LogError(ex, "Cleanup failed for command {CommandId}: {Error}", CommandId, errorMessage);
                return new TestCleanupResult(false, stopwatch.Elapsed, errorMessage, artifactsCleanedUp);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Cleanup failed for command: {CommandId}", CommandId);
                return new TestCleanupResult(false, stopwatch.Elapsed, ex.Message, artifactsCleanedUp);
            }
        }

        /// <summary>
        /// Internal validation logic to be implemented by derived classes.
        /// </summary>
        protected virtual Task ValidateInternalAsync(ITestContext context, List<string> errors, List<string> warnings, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Internal execution logic to be implemented by derived classes.
        /// </summary>
        protected abstract Task<bool> ExecuteInternalAsync(ITestContext context, Dictionary<string, object> outputData, List<TestArtifact> artifacts, CancellationToken cancellationToken);

        /// <summary>
        /// Internal cleanup logic to be implemented by derived classes.
        /// </summary>
        protected virtual Task CleanupInternalAsync(ITestContext context, TestExecutionResult result, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a test artifact.
        /// </summary>
        protected TestArtifact CreateArtifact(string name, TestArtifactType type, string filePath, bool shouldCleanup = true)
        {
            var fileInfo = new FileInfo(filePath);
            return new TestArtifact(
                name,
                type,
                filePath,
                fileInfo.Exists ? fileInfo.Length : 0,
                DateTimeOffset.UtcNow,
                shouldCleanup
            );
        }

        /// <summary>
        /// Gets CPU usage percentage.
        /// </summary>
        private double GetCpuUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets memory usage in bytes.
        /// </summary>
        private long GetMemoryUsage()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                return process.WorkingSet64;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the number of AI API calls from output data.
        /// </summary>
        private int GetAiApiCalls(Dictionary<string, object> outputData)
        {
            return outputData.TryGetValue("AiApiCalls", out var calls) && calls is int count ? count : 0;
        }

        /// <summary>
        /// Gets AI processing time from output data.
        /// </summary>
        private TimeSpan GetAiProcessingTime(Dictionary<string, object> outputData)
        {
            return outputData.TryGetValue("AiProcessingTime", out var time) && time is TimeSpan duration ? duration : TimeSpan.Zero;
        }

        /// <summary>
        /// Gets total file size from artifacts.
        /// </summary>
        private long GetTotalFileSize(List<TestArtifact> artifacts)
        {
            return artifacts.Sum(a => a.SizeBytes);
        }
    }
}
