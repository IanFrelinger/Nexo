using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation;

/// <summary>
/// Describe (sealed request) → Ingest (candidate) generation seam.
/// </summary>
public interface ISkillGenerator
{
    string BackendName { get; }

    bool IsolationEnforced { get; }

    int GenerationCallCount { get; }

    GenerationHandoff Describe(
        IntentDescriptor intent,
        IReadOnlyList<GateVerdictSummary>? priorVerdicts = null,
        int attemptIndex = 0,
        string? workRoot = null);

    Task<SkillCandidate> IngestAsync(GenerationHandoff handoff, CancellationToken ct = default);
}
