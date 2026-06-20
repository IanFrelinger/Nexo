namespace Nexo.Spike.S3.Models;

public enum EnsureSkillOutcome
{
    Reused,
    Generated,
    Rejected
}

public sealed record EnsureSkillResult(
    EnsureSkillOutcome Outcome,
    RegistryEntry? Entry,
    string? RejectionReason,
    bool GenerationRan,
    bool CertificationRan,
    bool Reused,
    string GeneratorBackend);
