using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;

namespace Nexo.Core.Tests.Pipeline
{
    /// <summary>
    /// Test implementation of ICanaryDeployer for testing purposes.
    /// </summary>
    /// <typeparam name="TArtifact">The type of artifact to deploy</typeparam>
    public class TestCanaryDeployer<TArtifact> : ICanaryDeployer<TArtifact>
    {
        private readonly ILogger<TestCanaryDeployer<TArtifact>> _logger;
        private readonly bool _shouldSucceed;
        private readonly string _canaryId;
        private readonly string _note;
        private int _callCount = 0;

        public TestCanaryDeployer(
            ILogger<TestCanaryDeployer<TArtifact>> logger,
            bool shouldSucceed = true,
            string? canaryId = null,
            string? note = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shouldSucceed = shouldSucceed;
            _canaryId = canaryId ?? $"canary-{Guid.NewGuid():N}";
            _note = note ?? "Test canary deployment";
        }

        public Task<(bool Healthy, string CanaryId, string Note)> CanaryAsync(
            TArtifact artifact, 
            CancellationToken ct)
        {
            _callCount++;
            _logger.LogInformation("TestCanaryDeployer called (attempt {Attempt})", _callCount);

            var healthy = _shouldSucceed;
            var actualNote = $"{_note} (attempt {_callCount})";

            _logger.LogInformation("TestCanaryDeployer result: Healthy={Healthy}, CanaryId={CanaryId}, Note={Note}", 
                healthy, _canaryId, actualNote);

            return Task.FromResult((healthy, _canaryId, actualNote));
        }

        public int CallCount => _callCount;
    }

    /// <summary>
    /// Test implementation that simulates canary failure for rollback testing.
    /// </summary>
    public class FailingCanaryDeployer<TArtifact> : ICanaryDeployer<TArtifact>
    {
        private readonly ILogger<FailingCanaryDeployer<TArtifact>> _logger;
        private int _callCount = 0;

        public FailingCanaryDeployer(ILogger<FailingCanaryDeployer<TArtifact>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<(bool Healthy, string CanaryId, string Note)> CanaryAsync(
            TArtifact artifact, 
            CancellationToken ct)
        {
            _callCount++;
            _logger.LogInformation("FailingCanaryDeployer called (attempt {Attempt})", _callCount);

            var canaryId = $"failed-canary-{Guid.NewGuid():N}";
            var note = "Canary deployment failed - simulated failure for testing";

            _logger.LogInformation("FailingCanaryDeployer result: Healthy=false, CanaryId={CanaryId}, Note={Note}", 
                canaryId, note);

            return Task.FromResult((false, canaryId, note));
        }

        public int CallCount => _callCount;
    }
}
