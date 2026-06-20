using Nexo.Spike.S3.Certification;
using Nexo.Spike.S3.Generation;
using Nexo.Spike.S3.Models;
using Nexo.Spike.S3.Registry;

namespace Nexo.Spike.S3.Loop;

/// <summary>
/// Lookup → generate (stand-in) → certify → admit reuse loop.
/// </summary>
public sealed class SkillReuseLoop
{
    private readonly SkillRegistry _registry;
    private readonly IStandInSkillGenerator _generator;
    private readonly ISkillCertificationHarness _certification;
    private int _certificationInvocationCount;

    public SkillReuseLoop(
        SkillRegistry registry,
        IStandInSkillGenerator generator,
        ISkillCertificationHarness certification)
    {
        _registry = registry;
        _generator = generator;
        _certification = certification;
    }

    public int CertificationInvocationCount => _certificationInvocationCount;

    public int GenerationCount => _generator.GenerationCount;

    public async Task<EnsureSkillResult> EnsureSkillAsync(
        IntentDescriptor intent,
        string? contextLabel = null,
        CancellationToken ct = default)
    {
        var registryBefore = _registry.ListEntries().Count;
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
                GeneratorBackend: _generator.BackendName);
        }

        var candidate = _generator.Generate(intent);
        _certificationInvocationCount++;
        var certification = await _certification.CertifyAsync(candidate, ct).ConfigureAwait(false);
        if (!certification.SatisfiesPromotionContract || certification.Record is null)
        {
            var registryAfterReject = _registry.ListEntries().Count;
            return new EnsureSkillResult(
                EnsureSkillOutcome.Rejected,
                null,
                certification.RejectionReason ?? "certification failed",
                GenerationRan: true,
                CertificationRan: true,
                Reused: false,
                GeneratorBackend: _generator.BackendName);
        }

        var admission = _registry.Admit(candidate, certification.Record);
        if (admission.Status != AdmissionStatus.Admitted || admission.Entry is null)
        {
            return new EnsureSkillResult(
                EnsureSkillOutcome.Rejected,
                null,
                admission.RejectionReason ?? "admission rejected",
                GenerationRan: true,
                CertificationRan: true,
                Reused: false,
                GeneratorBackend: _generator.BackendName);
        }

        _ = registryBefore;
        return new EnsureSkillResult(
            EnsureSkillOutcome.Generated,
            admission.Entry,
            null,
            GenerationRan: true,
            CertificationRan: true,
            Reused: false,
            GeneratorBackend: _generator.BackendName);
    }
}
