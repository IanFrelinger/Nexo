namespace Nexo.Spike.S3.Models;

/// <summary>
/// A generated or scripted brick candidate awaiting certification and admission.
/// </summary>
public sealed record SkillCandidate(
    string CandidateId,
    IntentDescriptor Intent,
    string IntentSpecPath,
    string ImplementationSource,
    string GeneratorBackend,
    string Hypothesis,
    SkillProvenance? Provenance = null);
