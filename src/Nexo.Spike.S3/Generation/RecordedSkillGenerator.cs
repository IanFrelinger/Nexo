using Nexo.Spike.S3.Models;

namespace Nexo.Spike.S3.Generation;

/// <summary>
/// Default offline recorded generator — deterministic catalog replay, isolationEnforced=false.
/// </summary>
public sealed class RecordedSkillGenerator : ISkillGenerator
{
    public const string BackendLabel = "recorded";

    private int _generationCallCount;

    public string BackendName => BackendLabel;

    public bool IsolationEnforced => false;

    public int GenerationCallCount => _generationCallCount;

    public GenerationHandoff Describe(
        IntentDescriptor intent,
        IReadOnlyList<GateVerdictSummary>? priorVerdicts = null,
        int attemptIndex = 0,
        string? workRoot = null)
    {
        var (request, _) = GenerationRequestSealer.BuildAsync(intent, priorVerdicts, attemptIndex)
            .GetAwaiter()
            .GetResult();

        var root = workRoot ?? Path.Combine(Path.GetTempPath(), "nexo-s3-recorded", Guid.NewGuid().ToString("N"));
        var handoffId = $"{BackendLabel}-{attemptIndex:D2}-{Guid.NewGuid():N}";
        var workDirectory = Path.Combine(root, handoffId);
        Directory.CreateDirectory(workDirectory);

        var requestPath = Path.Combine(workDirectory, GenerationRequestSealer.RequestFileName);
        GenerationRequestSealer.Persist(request, requestPath);
        RequestIsolationGuard.AssertIsolated(File.ReadAllText(requestPath), "recorded request.json");

        return new GenerationHandoff(
            handoffId,
            workDirectory,
            requestPath,
            request,
            IsolationEnforced: false,
            BackendLabel);
    }

    public Task<SkillCandidate> IngestAsync(GenerationHandoff handoff, CancellationToken ct = default)
    {
        _generationCallCount++;
        var catalog = StandInSkillCatalog.Resolve(handoff.Request.Intent);
        var (_, intentSpecPath) = GenerationRequestSealer
            .BuildAsync(handoff.Request.Intent, handoff.Request.PriorGateVerdicts, handoff.Request.AttemptIndex, ct)
            .GetAwaiter()
            .GetResult();

        var candidate = new SkillCandidate(
            catalog.CandidateId,
            handoff.Request.Intent,
            intentSpecPath,
            catalog.ImplementationSource,
            BackendLabel,
            catalog.Hypothesis,
            new SkillProvenance(BackendLabel, false, null, null, handoff.Request.AttemptIndex + 1));

        File.WriteAllText(
            Path.Combine(handoff.WorkDirectory, "candidate.json"),
            System.Text.Json.JsonSerializer.Serialize(candidate));

        return Task.FromResult(candidate);
    }
}
