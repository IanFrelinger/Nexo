using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;

namespace Nexo.Core.Tests.Pipeline
{
    /// <summary>
    /// Test implementation of IRollbackStrategy for testing purposes.
    /// </summary>
    /// <typeparam name="TArtifact">The type of artifact to rollback</typeparam>
    public partial class TestRollbackStrategy<TArtifact> : IRollbackStrategy<TArtifact>
    {
        private readonly ILogger<TestRollbackStrategy<TArtifact>> _logger;
        private readonly bool _shouldSucceed;
        private readonly string _note;
        private int _callCount = 0;

        public TestRollbackStrategy(
            ILogger<TestRollbackStrategy<TArtifact>> logger,
            bool shouldSucceed = true,
            string? note = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shouldSucceed = shouldSucceed;
            _note = note ?? "Test rollback completed";
        }

        public Task<string> RollbackAsync(
            TArtifact artifact, 
            string reason, 
            CancellationToken ct)
        {
            _callCount++;
            _logger.LogInformation("TestRollbackStrategy called (attempt {Attempt}) for reason: {Reason}", _callCount, reason);

            var result = _shouldSucceed 
                ? $"{_note} (attempt {_callCount}) - Reason: {reason}"
                : $"Rollback failed (attempt {_callCount}) - Reason: {reason}";

            _logger.LogInformation("TestRollbackStrategy result: {Result}", result);

            return Task.FromResult(result);
        }

        public int CallCount => _callCount;
    }

    /// <summary>
    /// Test implementation that simulates rollback failure for testing error handling.
    /// </summary>
    public partial class FailingRollbackStrategy<TArtifact> : IRollbackStrategy<TArtifact>
    {
        private readonly ILogger<FailingRollbackStrategy<TArtifact>> _logger;
        private int _callCount = 0;

        public FailingRollbackStrategy(ILogger<FailingRollbackStrategy<TArtifact>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> RollbackAsync(
            TArtifact artifact, 
            string reason, 
            CancellationToken ct)
        {
            _callCount++;
            _logger.LogInformation("FailingRollbackStrategy called (attempt {Attempt}) for reason: {Reason}", _callCount, reason);

            var result = $"Rollback failed (attempt {_callCount}) - Reason: {reason} - Simulated failure for testing";

            _logger.LogInformation("FailingRollbackStrategy result: {Result}", result);

            return Task.FromResult(result);
        }

        public int CallCount => _callCount;
    }
}
