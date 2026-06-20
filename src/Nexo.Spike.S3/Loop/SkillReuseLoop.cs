using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;

namespace Nexo.Spike.S3.Loop;

public sealed class SkillReuseLoopOptions
{
    public int MaxAttemptsPerIntent { get; init; } = 3;

    public string? GenerationWorkRoot { get; init; }
}

/// <summary>
/// Lookup → Describe → Ingest → certify → admit reuse loop with attempt cap.
/// </summary>
public sealed class SkillReuseLoop
{
    private readonly SkillRegistry _registry;
    private readonly ISkillGenerator _generator;
    private readonly ISkillCertificationHarness _certification;
    private readonly SkillReuseLoopOptions _options;
    private int _certificationInvocationCount;

    public SkillReuseLoop(
        SkillRegistry registry,
        ISkillGenerator generator,
        ISkillCertificationHarness certification,
        SkillReuseLoopOptions? options = null)
    {
        _registry = registry;
        _generator = generator;
        _certification = certification;
        _options = options ?? new SkillReuseLoopOptions();
    }

    public int CertificationInvocationCount => _certificationInvocationCount;

    public int GenerationCallCount => _generator.GenerationCallCount;

    public string GeneratorBackend => _generator.BackendName;

    public bool GeneratorIsolationEnforced => _generator.IsolationEnforced;

    public async Task<EnsureSkillResult> EnsureSkillAsync(
        IntentDescriptor intent,
        string? contextLabel = null,
        CancellationToken ct = default)
    {
        var lookup = _registry.Lookup(intent);
        if (lookup.Found)
        {
            return new EnsureSkillResult(
                EnsureSkillOutcome.Reused,
                lookup.Entry,
                null,
                GenerationRan: false,
                CertificationRan: false,
                Reused: true,
                GeneratorBackend: _generator.BackendName,
                IsolationEnforced: lookup.Entry?.Provenance.IsolationEnforced,
                ModelId: lookup.Entry?.Provenance.ModelId,
                AttemptsUsed: 0);
        }

        var priorVerdicts = new List<GateVerdictSummary>();
        var workRoot = _options.GenerationWorkRoot
                       ?? Path.Combine(Path.GetTempPath(), "nexo-s3-generate", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workRoot);

        for (var attempt = 0; attempt < _options.MaxAttemptsPerIntent; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var handoff = _generator.Describe(intent, priorVerdicts, attempt, workRoot);
            var candidate = await _generator.IngestAsync(handoff, ct).ConfigureAwait(false);

            _certificationInvocationCount++;
            var certification = await _certification.CertifyAsync(candidate, ct).ConfigureAwait(false);
            if (certification.SatisfiesPromotionContract && certification.Record is not null)
            {
                var admission = _registry.Admit(candidate, certification.Record);
                if (admission.Status == AdmissionStatus.Admitted && admission.Entry is not null)
                {
                    return new EnsureSkillResult(
                        EnsureSkillOutcome.Generated,
                        admission.Entry,
                        null,
                        GenerationRan: true,
                        CertificationRan: true,
                        Reused: false,
                        GeneratorBackend: _generator.BackendName,
                        IsolationEnforced: candidate.Provenance?.IsolationEnforced ?? _generator.IsolationEnforced,
                        ModelId: candidate.Provenance?.ModelId,
                        AttemptsUsed: attempt + 1);
                }

                return new EnsureSkillResult(
                    EnsureSkillOutcome.Rejected,
                    null,
                    admission.RejectionReason ?? "admission rejected",
                    GenerationRan: true,
                    CertificationRan: true,
                    Reused: false,
                    GeneratorBackend: _generator.BackendName,
                    IsolationEnforced: candidate.Provenance?.IsolationEnforced ?? _generator.IsolationEnforced,
                    ModelId: candidate.Provenance?.ModelId,
                    AttemptsUsed: attempt + 1);
            }

            priorVerdicts.Add(new GateVerdictSummary(
                attempt,
                ExtractFailedGate(certification.RejectionReason),
                SanitizeRejection(certification.RejectionReason)));

            if (attempt + 1 >= _options.MaxAttemptsPerIntent)
            {
                return new EnsureSkillResult(
                    EnsureSkillOutcome.Rejected,
                    null,
                    certification.RejectionReason ?? "certification failed after attempt cap",
                    GenerationRan: true,
                    CertificationRan: true,
                    Reused: false,
                    GeneratorBackend: _generator.BackendName,
                    IsolationEnforced: candidate.Provenance?.IsolationEnforced ?? _generator.IsolationEnforced,
                    ModelId: candidate.Provenance?.ModelId,
                    AttemptsUsed: attempt + 1);
            }
        }

        return new EnsureSkillResult(
            EnsureSkillOutcome.Rejected,
            null,
            "attempt cap exhausted without certified candidate",
            GenerationRan: true,
            CertificationRan: true,
            Reused: false,
            GeneratorBackend: _generator.BackendName,
            IsolationEnforced: _generator.IsolationEnforced,
            ModelId: null,
            AttemptsUsed: _options.MaxAttemptsPerIntent);
    }

    private static string? ExtractFailedGate(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;

        const string prefix = "gate stack failed at ";
        var index = reason.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var remainder = reason[(index + prefix.Length)..];
        var colon = remainder.IndexOf(':');
        return colon < 0 ? remainder.Trim() : remainder[..colon].Trim();
    }

    private static string? SanitizeRejection(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return null;

        var trimmed = reason.Trim();
        return trimmed.Length <= 160 ? trimmed : trimmed[..160] + "...";
    }
}
