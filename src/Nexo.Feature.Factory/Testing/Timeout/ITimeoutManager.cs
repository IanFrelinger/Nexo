using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Nexo.Feature.Factory.Testing.Timeout
{
    /// <summary>
    /// Interface for managing timeouts and preventing test execution from hanging.
    /// </summary>
    public interface ITimeoutManager
    {
        /// <summary>
        /// Creates a timeout cancellation token with escalation.
        /// </summary>
        /// <param name="timeout">The timeout duration</param>
        /// <param name="cancellationToken">The base cancellation token</param>
        /// <returns>A timeout cancellation token source</returns>
        TimeoutCancellationTokenSource CreateTimeoutToken(TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors a test execution with heartbeat checking.
        /// </summary>
        /// <param name="testId">The test identifier</param>
        /// <param name="executionTask">The test execution task</param>
        /// <param name="timeout">The timeout duration</param>
        /// <param name="cancellationToken">The base cancellation token</param>
        /// <returns>The test execution result</returns>
        Task<TestExecutionResult> MonitorTestExecutionAsync(
            string testId,
            Task<bool> executionTask,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Forces cancellation of a stuck test.
        /// </summary>
        /// <param name="testId">The test identifier</param>
        /// <param name="reason">The reason for force cancellation</param>
        void ForceCancelTest(string testId, string reason);

        /// <summary>
        /// Gets the current timeout configuration.
        /// </summary>
        /// <returns>The timeout configuration</returns>
        TimeoutConfiguration GetConfiguration();

        /// <summary>
        /// Updates the timeout configuration.
        /// </summary>
        /// <param name="configuration">The new timeout configuration</param>
        void UpdateConfiguration(TimeoutConfiguration configuration);
    }

    /// <summary>
    /// Represents a timeout cancellation token source with escalation.
    /// </summary>
    public sealed class TimeoutCancellationTokenSource : IDisposable
    {
        private readonly CancellationTokenSource _primaryCts;
        private readonly CancellationTokenSource _escalationCts;
        private readonly CancellationTokenSource _combinedCts;
        private readonly Timer _escalationTimer;
        private readonly ILogger _logger;
        private bool _disposed = false;

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        public CancellationToken Token => _combinedCts.Token;

        /// <summary>
        /// Gets whether the timeout has been triggered.
        /// </summary>
        public bool IsTimeoutTriggered => _primaryCts.IsCancellationRequested;

        /// <summary>
        /// Gets whether escalation has been triggered.
        /// </summary>
        public bool IsEscalationTriggered => _escalationCts.IsCancellationRequested;

        /// <summary>
        /// Initializes a new instance of the TimeoutCancellationTokenSource class.
        /// </summary>
        public TimeoutCancellationTokenSource(
            TimeSpan timeout,
            TimeSpan escalationTimeout,
            CancellationToken baseToken,
            ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _primaryCts = CancellationTokenSource.CreateLinkedTokenSource(baseToken);
            _escalationCts = new CancellationTokenSource();
            _combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_primaryCts.Token, _escalationCts.Token);

            // Set primary timeout
            _primaryCts.CancelAfter(timeout);

            // Set escalation timeout (longer than primary)
            _escalationTimer = new Timer(OnEscalationTimeout, null, (int)escalationTimeout.TotalMilliseconds, -1);

            _logger.LogDebug("Created timeout token: Primary={PrimaryTimeout}, Escalation={EscalationTimeout}", 
                timeout, escalationTimeout);
        }

        /// <summary>
        /// Cancels the timeout token.
        /// </summary>
        public void Cancel()
        {
            _primaryCts.Cancel();
        }

        /// <summary>
        /// Cancels the timeout token with a reason.
        /// </summary>
        /// <param name="reason">The cancellation reason</param>
        public void Cancel(string reason)
        {
            _logger.LogInformation("Cancelling timeout token: {Reason}", reason);
            _primaryCts.Cancel();
        }

        private void OnEscalationTimeout(object? state)
        {
            if (!_disposed && !_escalationCts.IsCancellationRequested)
            {
                _logger.LogWarning("Escalation timeout triggered - forcing cancellation");
                _escalationCts.Cancel();
            }
        }

        /// <summary>
        /// Disposes the timeout cancellation token source.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _escalationTimer?.Dispose();
                _primaryCts?.Dispose();
                _escalationCts?.Dispose();
                _combinedCts?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents timeout configuration.
    /// </summary>
    public sealed class TimeoutConfiguration
    {
        /// <summary>
        /// Gets or sets the default timeout duration.
        /// </summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the escalation timeout duration.
        /// </summary>
        public TimeSpan EscalationTimeout { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the heartbeat interval.
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the maximum number of heartbeat failures before force cancellation.
        /// </summary>
        public int MaxHeartbeatFailures { get; set; } = 3;

        /// <summary>
        /// Gets or sets whether to enable force cancellation.
        /// </summary>
        public bool EnableForceCancellation { get; set; } = true;

        /// <summary>
        /// Gets or sets the process timeout duration.
        /// </summary>
        public TimeSpan ProcessTimeout { get; set; } = TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// Represents a test execution result with timeout information.
    /// </summary>
    public sealed class TestExecutionResult
    {
        /// <summary>
        /// Gets whether the test execution was successful.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets the execution duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the error message if the test failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets whether the test timed out.
        /// </summary>
        public bool IsTimeout { get; }

        /// <summary>
        /// Gets whether the test was force cancelled.
        /// </summary>
        public bool IsForceCancelled { get; }

        /// <summary>
        /// Gets the cancellation reason.
        /// </summary>
        public string? CancellationReason { get; }

        /// <summary>
        /// Initializes a new instance of the TestExecutionResult class.
        /// </summary>
        public TestExecutionResult(
            bool isSuccess,
            TimeSpan duration,
            string? errorMessage = null,
            bool isTimeout = false,
            bool isForceCancelled = false,
            string? cancellationReason = null)
        {
            IsSuccess = isSuccess;
            Duration = duration;
            ErrorMessage = errorMessage;
            IsTimeout = isTimeout;
            IsForceCancelled = isForceCancelled;
            CancellationReason = cancellationReason;
        }
    }
}
