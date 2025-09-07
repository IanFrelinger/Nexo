using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Testing.Timeout;

namespace Nexo.Feature.Factory.Testing.Timeout
{
    /// <summary>
    /// Robust timeout manager that prevents test execution from hanging.
    /// </summary>
    public sealed class RobustTimeoutManager : ITimeoutManager
    {
        private readonly ILogger<RobustTimeoutManager> _logger;
        private readonly Dictionary<string, TimeoutCancellationTokenSource> _activeTests = new();
        private readonly Dictionary<string, Process?> _testProcesses = new();
        private readonly object _lock = new object();
        private TimeoutConfiguration _configuration = new();

        /// <summary>
        /// Initializes a new instance of the RobustTimeoutManager class.
        /// </summary>
        public RobustTimeoutManager(ILogger<RobustTimeoutManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a timeout cancellation token with escalation.
        /// </summary>
        public TimeoutCancellationTokenSource CreateTimeoutToken(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var escalationTimeout = TimeSpan.FromTicks(timeout.Ticks * 2); // Escalation is 2x the primary timeout
            return new TimeoutCancellationTokenSource(
                timeout,
                escalationTimeout,
                cancellationToken,
                _logger);
        }

        /// <summary>
        /// Monitors a test execution with heartbeat checking.
        /// </summary>
        public async Task<TestExecutionResult> MonitorTestExecutionAsync(
            string testId,
            Task<bool> executionTask,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTimeOffset.UtcNow;
            var timeoutToken = CreateTimeoutToken(timeout, cancellationToken);
            var heartbeatCts = new CancellationTokenSource();
            var processTimeoutCts = new CancellationTokenSource();

            try
            {
                _logger.LogInformation("Starting test execution monitoring for {TestId} with timeout {Timeout}", testId, timeout);

                // Register the test
                lock (_lock)
                {
                    _activeTests[testId] = timeoutToken;
                }

                // Start heartbeat monitoring
                var heartbeatTask = StartHeartbeatMonitoringAsync(testId, heartbeatCts.Token);

                // Start process timeout monitoring
                var processTimeoutTask = StartProcessTimeoutMonitoringAsync(testId, processTimeoutCts.Token);

                // Wait for test completion or timeout
                var completedTask = await Task.WhenAny(
                    executionTask,
                    Task.Delay(timeout, timeoutToken.Token),
                    heartbeatTask,
                    processTimeoutTask);

                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;

                if (completedTask == executionTask)
                {
                    // Test completed normally
                    var result = await executionTask;
                    _logger.LogInformation("Test {TestId} completed successfully in {Duration}", testId, duration);
                    return new TestExecutionResult(true, duration);
                }
                else if (completedTask == heartbeatTask)
                {
                    // Heartbeat monitoring detected issues
                    _logger.LogWarning("Test {TestId} failed heartbeat monitoring after {Duration}", testId, duration);
                    return new TestExecutionResult(false, duration, "Heartbeat monitoring failed", false, true, "Heartbeat failure");
                }
                else if (completedTask == processTimeoutTask)
                {
                    // Process timeout
                    _logger.LogWarning("Test {TestId} exceeded process timeout after {Duration}", testId, duration);
                    return new TestExecutionResult(false, duration, "Process timeout exceeded", false, true, "Process timeout");
                }
                else
                {
                    // Timeout occurred
                    _logger.LogWarning("Test {TestId} timed out after {Duration}", testId, duration);
                    return new TestExecutionResult(false, duration, $"Test timed out after {timeout.TotalSeconds:F1} seconds", true, false, "Timeout");
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                var endTime = DateTimeOffset.UtcNow;
                var duration = endTime - startTime;
                
                if (timeoutToken.IsTimeoutTriggered)
                {
                    _logger.LogWarning("Test {TestId} was cancelled due to timeout after {Duration}", testId, duration);
                    return new TestExecutionResult(false, duration, "Test execution was cancelled due to timeout", true, false, "Timeout cancellation");
                }
                else if (timeoutToken.IsEscalationTriggered)
                {
                    _logger.LogError("Test {TestId} was force cancelled due to escalation after {Duration}", testId, duration);
                    return new TestExecutionResult(false, duration, "Test execution was force cancelled due to escalation", false, true, "Escalation cancellation");
                }
                else
                {
                    _logger.LogInformation("Test {TestId} was cancelled after {Duration}", testId, duration);
                    return new TestExecutionResult(false, duration, "Test execution was cancelled", false, false, "User cancellation");
                }
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
                heartbeatCts.Cancel();
                processTimeoutCts.Cancel();
                timeoutToken.Dispose();

                lock (_lock)
                {
                    _activeTests.Remove(testId);
                    _testProcesses.Remove(testId);
                }
            }
        }

        /// <summary>
        /// Forces cancellation of a stuck test.
        /// </summary>
        public void ForceCancelTest(string testId, string reason)
        {
            lock (_lock)
            {
                if (_activeTests.TryGetValue(testId, out var cts))
                {
                    _logger.LogWarning("Force cancelling test {TestId}: {Reason}", testId, reason);
                    cts.Cancel();
                }

                if (_testProcesses.TryGetValue(testId, out var process) && process != null && !process.HasExited)
                {
                    try
                    {
                        _logger.LogWarning("Force killing process for test {TestId}: {Reason}", testId, reason);
                        process.Kill();
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
            _logger.LogInformation("Updated timeout configuration: Default={DefaultTimeout}, Escalation={EscalationTimeout}", 
                configuration.DefaultTimeout, configuration.EscalationTimeout);
        }

        private async Task StartHeartbeatMonitoringAsync(string testId, CancellationToken cancellationToken)
        {
            var heartbeatFailures = 0;
            var lastHeartbeat = DateTimeOffset.UtcNow;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_configuration.HeartbeatInterval, cancellationToken);

                    var currentTime = DateTimeOffset.UtcNow;
                    var timeSinceLastHeartbeat = currentTime - lastHeartbeat;

                    if (timeSinceLastHeartbeat > _configuration.HeartbeatInterval * 2)
                    {
                        heartbeatFailures++;
                        _logger.LogWarning("Heartbeat failure {FailureCount} for test {TestId} (last heartbeat: {LastHeartbeat})", 
                            heartbeatFailures, testId, lastHeartbeat);

                        if (heartbeatFailures >= _configuration.MaxHeartbeatFailures)
                        {
                            _logger.LogError("Maximum heartbeat failures reached for test {TestId}, forcing cancellation", testId);
                            ForceCancelTest(testId, "Heartbeat monitoring failure");
                            return;
                        }
                    }
                    else
                    {
                        heartbeatFailures = 0; // Reset on successful heartbeat
                        lastHeartbeat = currentTime;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        private async Task StartProcessTimeoutMonitoringAsync(string testId, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_configuration.ProcessTimeout, cancellationToken);
                
                // If we reach here, the process timeout has been exceeded
                _logger.LogError("Process timeout exceeded for test {TestId} after {ProcessTimeout}", testId, _configuration.ProcessTimeout);
                ForceCancelTest(testId, "Process timeout exceeded");
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
    }
}
