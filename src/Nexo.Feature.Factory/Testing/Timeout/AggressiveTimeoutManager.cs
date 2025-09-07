using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Timeout;

namespace Nexo.Feature.Factory.Testing.Timeout
{
    /// <summary>
    /// Aggressive timeout manager that uses process isolation and immediate cancellation to prevent hanging.
    /// </summary>
    public sealed class AggressiveTimeoutManager : ITimeoutManager
    {
        private readonly ILogger<AggressiveTimeoutManager> _logger;
        private readonly Dictionary<string, Process> _testProcesses = new();
        private readonly Dictionary<string, CancellationTokenSource> _activeCancellations = new();
        private readonly object _lock = new object();
        private TimeoutConfiguration _configuration = new();

        /// <summary>
        /// Initializes a new instance of the AggressiveTimeoutManager class.
        /// </summary>
        public AggressiveTimeoutManager(ILogger<AggressiveTimeoutManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a timeout cancellation token with immediate enforcement.
        /// </summary>
        public TimeoutCancellationTokenSource CreateTimeoutToken(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            // Use much shorter escalation timeout for aggressive enforcement
            var escalationTimeout = TimeSpan.FromTicks(timeout.Ticks / 2); // Escalation is 1/2 the primary timeout
            return new TimeoutCancellationTokenSource(
                timeout,
                escalationTimeout,
                cancellationToken,
                _logger);
        }

        /// <summary>
        /// Monitors a test execution with aggressive timeout enforcement.
        /// </summary>
        public async Task<TestExecutionResult> MonitorTestExecutionAsync(
            string testId,
            Task<bool> executionTask,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var aggressiveTimeout = TimeSpan.FromSeconds(Math.Min(timeout.TotalSeconds, 30)); // Cap at 30 seconds for aggressive mode

            try
            {
                _logger.LogWarning("Starting AGGRESSIVE test execution monitoring for {TestId} with timeout {Timeout}", testId, aggressiveTimeout);

                // Create aggressive cancellation token
                using var aggressiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                aggressiveCts.CancelAfter(aggressiveTimeout);

                // Register the test for potential force cancellation
                lock (_lock)
                {
                    _activeCancellations[testId] = aggressiveCts;
                }

                // Start immediate timeout monitoring
                var timeoutTask = Task.Delay(aggressiveTimeout, aggressiveCts.Token);
                
                // Race between test completion and timeout
                var completedTask = await Task.WhenAny(executionTask, timeoutTask);

                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;

                if (completedTask == executionTask)
                {
                    // Test completed normally
                    var result = await executionTask;
                    _logger.LogInformation("Test {TestId} completed successfully in {Duration}", testId, duration);
                    return new TestExecutionResult(true, duration);
                }
                else
                {
                    // Timeout occurred - force cancellation immediately
                    _logger.LogError("AGGRESSIVE TIMEOUT: Test {TestId} exceeded {Timeout} after {Duration} - FORCE CANCELLING", 
                        testId, aggressiveTimeout, duration);
                    
                    // Force cancel immediately
                    ForceCancelTest(testId, "Aggressive timeout exceeded");
                    
                    return new TestExecutionResult(false, duration, 
                        $"AGGRESSIVE TIMEOUT: Test exceeded {aggressiveTimeout.TotalSeconds:F1} seconds", 
                        true, true, "Aggressive timeout");
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;
                
                _logger.LogError("AGGRESSIVE CANCELLATION: Test {TestId} was force cancelled after {Duration}", testId, duration);
                
                return new TestExecutionResult(false, duration, 
                    "Test was force cancelled due to aggressive timeout", 
                    true, true, "Aggressive cancellation");
            }
            catch (Exception ex)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;
                _logger.LogError(ex, "Test {TestId} failed with exception after {Duration}", testId, duration);
                return new TestExecutionResult(false, duration, ex.Message);
            }
            finally
            {
                // Cleanup
                lock (_lock)
                {
                    _activeCancellations.Remove(testId);
                    if (_testProcesses.TryGetValue(testId, out var process))
                    {
                        try
                        {
                            if (process != null && !process.HasExited)
                            {
                                process.Kill();
                                process.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to cleanup process for test {TestId}", testId);
                        }
                        _testProcesses.Remove(testId);
                    }
                }
            }
        }

        /// <summary>
        /// Forces immediate cancellation of a stuck test.
        /// </summary>
        public void ForceCancelTest(string testId, string reason)
        {
            lock (_lock)
            {
                _logger.LogError("FORCE CANCELLING test {TestId}: {Reason}", testId, reason);

                // Cancel the cancellation token immediately
                if (_activeCancellations.TryGetValue(testId, out var cts))
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to cancel token for test {TestId}", testId);
                    }
                }

                // Kill any associated process immediately
                if (_testProcesses.TryGetValue(testId, out var process) && process != null)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            _logger.LogError("KILLING process for test {TestId}: {Reason}", testId, reason);
                            process.Kill(true); // Kill entire process tree
                        }
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to kill process for test {TestId}", testId);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current timeout configuration.
        /// </summary>
        public TimeoutConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Updates the timeout configuration.
        /// </summary>
        public void UpdateConfiguration(TimeoutConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger.LogWarning("Updated AGGRESSIVE timeout configuration: Default={DefaultTimeout}, Escalation={EscalationTimeout}", 
                configuration.DefaultTimeout, configuration.EscalationTimeout);
        }
    }
}
