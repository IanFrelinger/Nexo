using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Contracts;

namespace Nexo.Core.Tests.Pipeline
{
    /// <summary>
    /// Test implementation of IRepairStrategy for testing purposes.
    /// </summary>
    /// <typeparam name="TRequest">The type of request for generation</typeparam>
    /// <typeparam name="TArtifact">The type of artifact generated</typeparam>
    public partial class TestRepairStrategy<TRequest, TArtifact> : IRepairStrategy<TRequest, TArtifact>
    {
        private readonly ILogger<TestRepairStrategy<TRequest, TArtifact>> _logger;
        private readonly bool _shouldSucceed;
        private readonly string _patchedSource;
        private readonly string _note;
        private int _callCount = 0;

        public TestRepairStrategy(
            ILogger<TestRepairStrategy<TRequest, TArtifact>> logger,
            bool shouldSucceed = true,
            string? patchedSource = null,
            string? note = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shouldSucceed = shouldSucceed;
            _patchedSource = patchedSource ?? "// Repaired source code";
            _note = note ?? "Test repair applied";
        }

        public Task<(bool Applied, string PatchedSource, string Note)> TryRepairAsync(
            TRequest request, 
            GenerationResult<TArtifact> lastGen, 
            IReadOnlyList<ValidationResult> gateFindings, 
            PolicyOutcome? policy, 
            CancellationToken ct)
        {
            _callCount++;
            _logger.LogInformation("TestRepairStrategy called (attempt {Attempt})", _callCount);

            // Simulate repair logic based on findings
            var applied = _shouldSucceed && _callCount <= 2; // Succeed on first 2 attempts
            var actualNote = $"{_note} (attempt {_callCount})";

            _logger.LogInformation("TestRepairStrategy result: Applied={Applied}, Note={Note}", applied, actualNote);

            return Task.FromResult((applied, _patchedSource, actualNote));
        }

        public int CallCount => _callCount;
    }

    /// <summary>
    /// Test implementation that simulates a failing gate that becomes passing after repair.
    /// </summary>
    public partial class FailingGateRepairStrategy<TRequest, TArtifact> : IRepairStrategy<TRequest, TArtifact>
    {
        private readonly ILogger<FailingGateRepairStrategy<TRequest, TArtifact>> _logger;
        private int _callCount = 0;

        public FailingGateRepairStrategy(ILogger<FailingGateRepairStrategy<TRequest, TArtifact>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<(bool Applied, string PatchedSource, string Note)> TryRepairAsync(
            TRequest request, 
            GenerationResult<TArtifact> lastGen, 
            IReadOnlyList<ValidationResult> gateFindings, 
            PolicyOutcome? policy, 
            CancellationToken ct)
        {
            _callCount++;
            _logger.LogInformation("FailingGateRepairStrategy called (attempt {Attempt})", _callCount);

            // Simulate repair that fixes the issue on first attempt
            var applied = _callCount == 1;
            var patchedSource = lastGen.SourceCode + "\n// Fixed by repair strategy";
            var note = applied ? "Gate failure fixed on first repair attempt" : "Repair failed";

            _logger.LogInformation("FailingGateRepairStrategy result: Applied={Applied}, Note={Note}", applied, note);

            return Task.FromResult((applied, patchedSource, note));
        }

        public int CallCount => _callCount;
    }
}
